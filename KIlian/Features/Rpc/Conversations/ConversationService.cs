using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KIlian.EfCore;
using KIlian.EfCore.Entities;
using KIlian.Features.Rpc.Pagination.Extensions;
using KIlian.Generated.Rpc.Conversations;
using Microsoft.EntityFrameworkCore;
using Conversation = KIlian.Generated.Rpc.Conversations.Conversation;

namespace KIlian.Features.Rpc.Conversations;

public class ConversationService(IDbContextFactory<KIlianSqliteDbContext> dbFactory) : Conversation.ConversationBase
{
    public override async Task<OffsetPaginatedMessagesDto> GetMessages(GetOffsetPaginatedMessagesDto request, ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        
        var messages = await db.Set<Message>().OrderBy(m => m.Created).ThenBy(m => m.Id).Skip(request.Pagination.GetOffset()).Take(request.Pagination.GetCount()).ToArrayAsync(context.CancellationToken);
        return new()
        {
            Paginated = new() { TotalItemsCount = await db.Set<Message>().CountAsync(context.CancellationToken) },
            Messages = 
            { 
                messages.Select(msg => new MessageDto
                {
                    Id = msg.Id,
                    Created = Timestamp.FromDateTimeOffset(msg.Created),
                    From = msg.From,
                    Content = msg.Content,
                })
            }
        };
    }

    public override async Task<Empty> DeleteMessages(DeleteMessagesDto request, ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        
        db.RemoveRange(db.Set<Message>().Where(message => request.MessageIds.Contains(message.Id)));
        await db.SaveChangesAsync(context.CancellationToken);
        return new();
    }

    public override async Task<ConversationDto> CreateConversationFromTurns(CreateConversationFromTurnsDto request, ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        
        var conversationEntry = await db.AddAsync(new KIlian.EfCore.Entities.Conversation
        {
            Turns = request.Turns.OrderBy(turn => turn.Order).Select((turn, index) => new ConversationTurn
            {
                Content = turn.Content,
                From = index % 2 == 0 ? ConversationParticipant.User : ConversationParticipant.Assistant,
                Order = turn.Order,
            }).ToList(),
        }, context.CancellationToken);
        
        await db.SaveChangesAsync(context.CancellationToken);

        return new()
        {
            Id = conversationEntry.Entity.Id,
            Turns =
            {
                conversationEntry.Entity.Turns.OrderBy(turn => turn.Order).Select(turn => new ConversationTurnDto
                {
                    Id = turn.Id,
                    Content = turn.Content,
                    From = (ConversationParticipantDto)turn.From,
                    Order = turn.Order,
                })
            }
        };
    }

    public override async Task GenerateTrainingData(GenerateTrainingDataDto request, IServerStreamWriter<TrainingDataDto> responseStream,
        ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);

        IQueryable<ConversationTurn> turns = db.Set<ConversationTurn>().OrderBy(turn => turn.ConversationId).ThenBy(turn => turn.Order);
        
        if (request.HasAmountOfConversations)
        {
            turns = turns.Where(turn => db.Set<KIlian.EfCore.Entities.Conversation>().OrderBy(_ => EF.Functions.Random()).Take(request.AmountOfConversations).Select(c => c.Id).Contains(turn.ConversationId));
        }
        
        var buffer = new ArrayBufferWriter<byte>();
        await using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WritePropertyName("conversations");
        writer.WriteStartArray();
        
        var serializerOptions = new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        };
        
        foreach (var conversationTurns in turns.GroupBy(turn => turn.ConversationId).AsSingleQuery())
        {
            writer.WriteRawValue(JsonSerializer.Serialize(conversationTurns.Select(turn => new
            {
                from = turn.From,
                content = turn.Content,
            }).ToArray(), serializerOptions));

            if (buffer.WrittenCount <= 0)
            {
                continue;
            }

            await responseStream.WriteAsync(new() { Chunk = ByteString.CopyFrom(buffer.WrittenSpan) }, context.CancellationToken);
            buffer.Clear();
        }
        
        writer.WriteEndArray();
        writer.WriteEndObject();
        
        await writer.FlushAsync(context.CancellationToken);
        await responseStream.WriteAsync(new() { Chunk = ByteString.CopyFrom(buffer.WrittenSpan) }, context.CancellationToken);
    }
}