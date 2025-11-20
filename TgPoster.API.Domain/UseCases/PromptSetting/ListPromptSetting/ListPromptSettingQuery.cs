using MediatR;
using TgPoster.API.Domain.UseCases.Messages.ListMessage;

namespace TgPoster.API.Domain.UseCases.PromptSetting.ListPromptSetting;

public record ListPromptSettingQuery(int PageNumber, int PageSize) : IRequest<PagedResponse<PromptSettingResponse>>;