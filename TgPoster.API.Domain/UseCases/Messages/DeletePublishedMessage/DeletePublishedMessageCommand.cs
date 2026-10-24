using MediatR;

namespace TgPoster.API.Domain.UseCases.Messages.DeletePublishedMessage;

/// <summary>
/// Удалить опубликованный пост из канала и из списка постов
/// </summary>
/// <param name="Id"></param>
public sealed record DeletePublishedMessageCommand(Guid Id) : IRequest;
