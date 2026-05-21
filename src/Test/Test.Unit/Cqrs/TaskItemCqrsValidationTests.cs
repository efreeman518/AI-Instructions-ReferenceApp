using EF.Common.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using EF.CQRS.Abstractions;
using TaskFlow.Application.Cqrs.Requests;
using EF.CQRS.Decorators;
using EF.CQRS.Validation;
using TaskFlow.Application.Cqrs.Validation;
using TaskFlow.Application.Models;
using Test.Support;

namespace Test.Unit.Cqrs;

[TestClass]
public sealed class TaskItemCqrsValidationTests
{
    [TestMethod]
    public async Task Given_InvalidTaskItemCreate_When_ValidationDecoratorRuns_Then_HandlerIsNotCalled()
    {
        var tenantId = TestConstants.TenantId;
        var requestContext = new Mock<IRequestContext<string, Guid?>>();
        requestContext.SetupGet(x => x.TenantId).Returns(tenantId);
        requestContext.SetupGet(x => x.Roles).Returns(new List<string>());

        var inner = new TrackingHandler();
        var validator = new CreateTaskItemCommandValidator(requestContext.Object);
        var decorator = new ValidationRequestHandlerDecorator<CreateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>>(
            inner,
            [validator],
            new StaticFailureValidationResponseFactory<Result<DefaultResponse<TaskItemDto>>>(),
            NullLogger<ValidationRequestHandlerDecorator<CreateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>>>.Instance);

        var command = new CreateTaskItemCommand(new DefaultRequest<TaskItemDto>
        {
            Item = new TaskItemDto { Title = "" }
        });

        var result = await decorator.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.IsFalse(inner.WasCalled);
        Assert.AreEqual(tenantId, command.Request.Item.TenantId);
        StringAssert.Contains(string.Join(";", result.Errors), "Title is required");
    }

    private sealed class TrackingHandler
        : IRequestHandler<CreateTaskItemCommand, Result<DefaultResponse<TaskItemDto>>>
    {
        public bool WasCalled { get; private set; }

        public Task<Result<DefaultResponse<TaskItemDto>>> HandleAsync(
            CreateTaskItemCommand request,
            CancellationToken ct = default)
        {
            WasCalled = true;
            return Task.FromResult(Result<DefaultResponse<TaskItemDto>>.Success(new DefaultResponse<TaskItemDto>
            {
                Item = request.Request.Item
            }));
        }
    }
}
