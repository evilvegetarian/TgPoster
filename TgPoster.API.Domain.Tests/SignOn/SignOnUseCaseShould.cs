using Moq;
using Moq.Language.Flow;
using Security.Interfaces;
using Shouldly;
using TgPoster.API.Domain.UseCases.Accounts.SignOn;

namespace TgPoster.API.Domain.Tests.SignOn;

public class SignOnUseCaseShould
{
    private readonly ISetup<ISignOnStorage, Task<Guid>> createUserSetup;
    private readonly ISetup<IPasswordHasher, string> generatePasswordPartsSetup;
    private readonly ISetup<ISignOnStorage, Task<bool>> haveUserNameSetup;
    private readonly Mock<ISignOnStorage> storage;
    private readonly SignOnUseCase sut;

    public SignOnUseCaseShould()
    {
        var passwordManager = new Mock<IPasswordHasher>();
        generatePasswordPartsSetup = passwordManager.Setup(m
            => m.Generate(It.IsAny<string>()));

        storage = new Mock<ISignOnStorage>();
        createUserSetup = storage.Setup(s =>
            s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));

        haveUserNameSetup = storage.Setup(s =>
            s.HaveUserNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        sut = new SignOnUseCase(passwordManager.Object, storage.Object);
    }

    [Fact]
    public async Task CreateUser_WithGeneratedPasswordParts()
    {
        var hash = "asf3r23faefa";
        generatePasswordPartsSetup.Returns(hash);
        haveUserNameSetup.ReturnsAsync(false);

        var command = new SignOnCommand("Test", "qwerty");
        await sut.Handle(command, CancellationToken.None);

        storage.Verify(s => s.HaveUserNameAsync(command.Login, CancellationToken.None), Times.Once);
        storage.Verify(s => s.CreateUserAsync(command.Login, hash, It.IsAny<CancellationToken>()), Times.Once);
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReturnIdentityOfNewlyCreatedUser()
    {
        var id = Guid.Parse("a3fbad24-9607-407d-9dac-3517861ff421");
        generatePasswordPartsSetup.Returns("asf3r23faefa");
        createUserSetup.ReturnsAsync(id);

        var actual = await sut.Handle(new SignOnCommand("Test", "password"), CancellationToken.None);
        actual.UserId.ShouldBe(id);
    }

    [Fact]
    public async Task ThrowException_WhenUserAlreadyExists()
    {
        generatePasswordPartsSetup.Returns("asf3r23faefa");
        haveUserNameSetup.ReturnsAsync(true);

        var command = new SignOnCommand("ExistingUser", "password123");

        await Should.ThrowAsync<Exception>(async () => await sut.Handle(command, It.IsAny<CancellationToken>()));

        storage.Verify(s =>
            s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}