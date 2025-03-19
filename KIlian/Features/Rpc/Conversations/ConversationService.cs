using System.Text.Encodings.Web;
using System.Text.Json;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KIlian.EfCore;
using KIlian.EfCore.Entities;
using KIlian.Features.Rpc.Pagination.Extensions;
using KIlian.Generated.Rpc.Conversations;
using Microsoft.EntityFrameworkCore;
using Conversation = KIlian.Generated.Rpc.Conversations.Conversation;
using KIlianConversation = KIlian.EfCore.Entities.Conversation;

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
        
        var conversationEntry = await db.AddAsync(new KIlianConversation
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
            // ReSharper disable once EntityFramework.ClientSideDbFunctionCall
            // ReSharper disable once AccessToDisposedClosure
            turns = turns.Where(turn => db.Set<KIlianConversation>().OrderBy(_ => EF.Functions.Random()).Take(request.AmountOfConversations).Select(c => c.Id).Contains(turn.ConversationId));
        }
        
        await using var stream = new GrpcResponseStream(responseStream);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        writer.WriteStartObject();
        writer.WritePropertyName("conversations");
        writer.WriteStartArray();
        
        foreach (var conversationTurns in turns.GroupBy(turn => turn.ConversationId).AsSingleQuery())
        {
            writer.WriteStartArray();
            foreach (var turn in conversationTurns)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("role");
                writer.WriteStringValue(turn.From.ToString().ToLowerInvariant());
                
                writer.WritePropertyName("content");
                writer.WriteStringValue(turn.Content);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public override async Task<OffsetPaginatedConversationsDto> GetConversations(GetOffsetPaginatedConversationsDto request, ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        
        var count = await db.Set<KIlianConversation>().CountAsync(context.CancellationToken);

        var conversations = await db.Set<KIlianConversation>()
            .Skip(request.Pagination.Offset)
            .Take(request.Pagination.Count)
            .Include(c => c.Turns)
            .OrderBy(c => c.Id)
            .AsSingleQuery()
            .ToArrayAsync(context.CancellationToken);

        return new OffsetPaginatedConversationsDto
        {
            Paginated = new()
            {
                TotalItemsCount = count
            },
            Conversations =
            {
                conversations.Select(c => new ConversationDto
                {
                    Id = c.Id,
                    Turns =
                    {
                        c.Turns.Select(turn => new ConversationTurnDto
                        {
                            Id = turn.Id,
                            Content = turn.Content,
                            From = (ConversationParticipantDto)turn.From,
                            Order = turn.Order,
                        })
                    }
                })
            }
        };
    }

    public override async Task<Empty> DeleteConversations(DeleteConversationsDto request, ServerCallContext context)
    {
        await using var db = await dbFactory.CreateDbContextAsync(context.CancellationToken);
        db.RemoveRange(db.Set<KIlianConversation>().Where(convo => request.ConversationIds.Contains(convo.Id)));
        return new();
    }
}