using MediatR;

namespace TgPoster.API.Domain.UseCases.Parse.ListParseChannel;

public sealed class ListParseChannelsQuery : IRequest<List<ParseChannelsResponse>>;