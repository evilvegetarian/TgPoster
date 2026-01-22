namespace Security.Authentication;

public sealed record TokenServiceBuildTokenPayload(
	Guid UserId
);