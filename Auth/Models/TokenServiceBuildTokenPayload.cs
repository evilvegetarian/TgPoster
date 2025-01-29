namespace Auth.Models;

public sealed record TokenServiceBuildTokenPayload(
    Guid UserId
);