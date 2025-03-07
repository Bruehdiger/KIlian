using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using KIlian.EfCore;
using KIlian.EfCore.Entities;
using KIlian.Features.Rpc.Pagination.Extensions;
using KIlian.Generated.Rpc.Conversations;
using Microsoft.EntityFrameworkCore;
using Conversation = KIlian.Generated.Rpc.Conversations.Conversation;

namespace KIlian.Features.Rpc.Conversations;

public class ConversationService(KIlianSqliteDbContext db) : Conversation.ConversationBase
{
    public override async Task<OffsetPaginatedMessagesResponse> GetMessages(OffsetPaginatedMessagesRequest request, ServerCallContext context)
    {
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
}