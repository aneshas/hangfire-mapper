using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using FluentAssertions;
using Hangfire.Batches.States;
using Hangfire.Common;
using Hangfire.Mapper.Tests.DummyJob;
using Hangfire.States;
using Moq;
using Xunit;

namespace Hangfire.Mapper.Tests
{
    public class MapperJobTests
    {
        [Fact]
        public void QueryIsCalledWithInitialState()
        {
            var initialPage = 1;

            var repo = new Mock<IResourceRepository>();

            var job = new DummyMapperJob(null, repo.Object);

            job.Enqueue(new DummyMapperJob.State {Page = initialPage});

            repo.Verify(r => r.List(initialPage), Times.Once);
        }

        [Fact]
        public void InitialBatchIsEnqueuedWithIndividualChildJobs()
        {
            var initialPage = 1;

            var repo = new Mock<IResourceRepository>();

            var res1 = new Resource() {Id = 1, Name = "John"};
            var res2 = new Resource() {Id = 2, Name = "Jane"};
            var res3 = new Resource() {Id = 3, Name = "Max"};

            var resources = new[] {res1, res2, res3};

            repo.Setup(r => r.List(initialPage)).ReturnsAsync(resources);

            var batchClient = new Mock<IBatchJobClient>();

            var job = new DummyMapperJob(batchClient.Object, repo.Object);

            Action<IBatchAction> action = null;

            batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchStartedState>(), It.IsAny<string>()))
                .Callback<Action<IBatchAction>, IBatchState, string>((a, state, d) => action = a)
                .Returns("initial-batch-id");

            job.Enqueue(new DummyMapperJob.State {Page = initialPage});

            var batchAction = new Mock<IBatchAction>();

            action(batchAction.Object);

            batchAction.Invocations.Should().SatisfyRespectively(
                jobA =>
                {
                    var enqueuedJob = jobA.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(res1);
                    jobA.Arguments[1].Should().BeOfType<EnqueuedState>();
                },
                jobB =>
                {
                    var enqueuedJob = jobB.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(res2);
                    jobB.Arguments[1].Should().BeOfType<EnqueuedState>();
                },
                jobC =>
                {
                    var enqueuedJob = jobC.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(res3);
                    jobC.Arguments[1].Should().BeOfType<EnqueuedState>();
                });
        }

        [Fact]
        public void NextBatchIsEnqueued()
        {
            var initialState = new DummyMapperJob.State {Page = 1};
            var nextState = new DummyMapperJob.State {Page = 2};

            var repo = new Mock<IResourceRepository>();

            var res1 = new Resource() {Id = 1, Name = "John"};
            var res2 = new Resource() {Id = 2, Name = "Jane"};
            var res3 = new Resource() {Id = 3, Name = "Max"};

            var resources = new[] {res1, res2, res3};

            repo.Setup(r => r.List(initialState.Page)).ReturnsAsync(resources);

            var batchClient = new Mock<IBatchJobClient>();

            var job = new DummyMapperJob(batchClient.Object, repo.Object);

            Action<IBatchAction> action = null;

            batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchStartedState>(), It.IsAny<string>()))
                .Returns("initial-batch-id");

            batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchAwaitingState>(), null))
                .Callback<Action<IBatchAction>, IBatchState, string>((a, state, d) => action = a);

            job.Enqueue(initialState);

            var batchAction = new Mock<IBatchAction>();

            action(batchAction.Object);

            batchAction.Invocations.Should().SatisfyRespectively(
                nextBatch =>
                {
                    var enqueuedJob = nextBatch.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    // TODO - Check parent id 
                    enqueuedJob.Args[0].Should().BeEquivalentTo(nextState);
                    nextBatch.Arguments[1].Should().BeOfType<EnqueuedState>();
                });
        }

        [Fact]
        public void NoJobsAreEnqueuedIfQueryReturnsNull()
        {
            var repo = new Mock<IResourceRepository>();

            repo.Setup(r => r.List(It.IsAny<int>())).ReturnsAsync((IEnumerable<Resource>) null);

            var batchClient = new Mock<IBatchJobClient>();

            var job = new DummyMapperJob(batchClient.Object, repo.Object);

            job.Enqueue(new DummyMapperJob.State());

            batchClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void NoJobsAreEnqueuedIfQueryReturnsEmptySet()
        {
            var repo = new Mock<IResourceRepository>();

            repo.Setup(r => r.List(It.IsAny<int>())).ReturnsAsync(new List<Resource>());

            var batchClient = new Mock<IBatchJobClient>();

            var job = new DummyMapperJob(batchClient.Object, repo.Object);

            job.Enqueue(new DummyMapperJob.State());

            batchClient.VerifyNoOtherCalls();
        }
    }
}