using System.Buffers;
using System.Collections.Concurrent;
using System.Text.Json;
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

        var totalConversationCount = await db.Set<KIlian.EfCore.Entities.Conversation>().CountAsync(context.CancellationToken);

        IQueryable<KIlian.EfCore.Entities.Conversation> conversationsQuery = db
            .Set<KIlian.EfCore.Entities.Conversation>()
            // ReSharper disable once EntityFramework.ClientSideDbFunctionCall
            .OrderBy(_ => EF.Functions.Random())
            .Include(c => c.Turns.OrderBy(turn => turn.Order));
        
        if (request.HasAmountOfConversations)
        {
            conversationsQuery = conversationsQuery.Take(request.AmountOfConversations);
        }

        var buffer = new ArrayBufferWriter<byte>();
        await using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WritePropertyName("conversations");
        writer.WriteStartArray();
        
        foreach (var conversation in conversationsQuery.AsSplitQuery())
        {
            writer.WriteRawValue(JsonSerializer.Serialize(conversation.Turns.Select(turn => new
            {
                from = turn.From,
                content = turn.Content,
            }).ToArray()));

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