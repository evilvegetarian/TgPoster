using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.ListChannel;

public sealed class ListParseChannelsQuery : IRequest<List<ParseChannelsResponse>>;