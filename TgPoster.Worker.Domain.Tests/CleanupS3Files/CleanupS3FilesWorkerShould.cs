using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using TgPoster.Worker.Domain.ConfigModels;
using TgPoster.Worker.Domain.UseCases.CleanupS3Files;

namespace TgPoster.Worker.Domain.Tests.CleanupS3Files;

public sealed class CleanupS3FilesWorkerShould
{
	private readonly Mock<IAmazonS3> s3 = new();
	private readonly Mock<ICleanupS3FilesStorage> storage = new();
	private readonly CleanupS3FilesWorker sut;

	public CleanupS3FilesWorkerShould()
	{
		sut = new(s3.Object, new S3Options
		{
			AccessKey = "test", SecretKey = "test", ServiceUrl = "https://s3.example.test", BucketName = "test"
		}, storage.Object, NullLogger<CleanupS3FilesWorker>.Instance, Mock.Of<IHostApplicationLifetime>());
	}

	[Fact]
	public async Task KeepRecentObjects_AndObjectsWithUnknownAge()
	{
		SetupObjects(new S3Object { Key = "recent", LastModified = DateTime.UtcNow.AddDays(-9) },
			new S3Object { Key = "unknown" });

		await sut.CleanupAsync();

		s3.Verify(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task DeleteOldObjectsInBatches_AndResetOnlyTheirFileFlags()
	{
		var ids = Enumerable.Range(0, 1001).Select(_ => Guid.NewGuid()).ToArray();
		SetupObjects(ids.Select(id => new S3Object { Key = id.ToString(), LastModified = DateTime.UtcNow.AddDays(-11) }).ToArray());
		var sizes = new List<int>();
		s3.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((DeleteObjectsRequest request, CancellationToken _) =>
			{
				sizes.Add(request.Objects.Count);
				request.BucketName.ShouldBe("test");
				return new DeleteObjectsResponse { DeletedObjects = request.Objects.Select(x => new DeletedObject { Key = x.Key }).ToList() };
			});

		await sut.CleanupAsync();

		sizes.ShouldBe([1000, 1]);
		storage.Verify(x => x.ResetIsInS3FlagAsync(It.Is<IReadOnlyCollection<Guid>>(values => values.Order().SequenceEqual(ids.Order())),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ReadAllPages_AndDeleteObjectsWhoseKeysAreNotFileIds()
	{
		s3.Setup(x => x.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.ContinuationToken == null),
			It.IsAny<CancellationToken>())).ReturnsAsync(new ListObjectsV2Response { IsTruncated = true, NextContinuationToken = "next" });
		s3.Setup(x => x.ListObjectsV2Async(It.Is<ListObjectsV2Request>(r => r.ContinuationToken == "next"),
			It.IsAny<CancellationToken>())).ReturnsAsync(new ListObjectsV2Response
		{
			S3Objects = [new S3Object { Key = "old-object", LastModified = DateTime.UtcNow.AddDays(-11) }]
		});
		s3.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteObjectsResponse { DeletedObjects = [new DeletedObject { Key = "old-object" }] });

		await sut.CleanupAsync();

		s3.Verify(x => x.DeleteObjectsAsync(It.Is<DeleteObjectsRequest>(r => r.Objects.Single().Key == "old-object"),
			It.IsAny<CancellationToken>()), Times.Once);
		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task KeepFileFlags_WhenDeletionFails()
	{
		SetupObjects(new S3Object { Key = Guid.NewGuid().ToString(), LastModified = DateTime.UtcNow.AddDays(-11) });
		s3.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new AmazonS3Exception("unavailable"));

		await Should.ThrowAsync<AmazonS3Exception>(() => sut.CleanupAsync());

		storage.VerifyNoOtherCalls();
	}

	[Fact]
	public async Task KeepFlagOfFailedObject_WhenS3ReturnsPartialErrors()
	{
		var deleted = Guid.NewGuid();
		var failed = Guid.NewGuid();
		SetupObjects(new S3Object { Key = deleted.ToString(), LastModified = DateTime.UtcNow.AddDays(-11) },
			new S3Object { Key = failed.ToString(), LastModified = DateTime.UtcNow.AddDays(-11) });
		s3.Setup(x => x.DeleteObjectsAsync(It.IsAny<DeleteObjectsRequest>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new DeleteObjectsResponse
			{
				DeletedObjects = [new DeletedObject { Key = deleted.ToString() }],
				DeleteErrors = [new DeleteError { Key = failed.ToString(), Code = "AccessDenied" }]
			});

		await sut.CleanupAsync();

		storage.Verify(x => x.ResetIsInS3FlagAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(deleted)),
			It.IsAny<CancellationToken>()), Times.Once);
	}

	private void SetupObjects(params S3Object[] objects) =>
		s3.Setup(x => x.ListObjectsV2Async(It.IsAny<ListObjectsV2Request>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new ListObjectsV2Response { S3Objects = [..objects] });
}
