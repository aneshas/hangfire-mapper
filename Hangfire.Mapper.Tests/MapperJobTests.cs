using System;
using System.Collections.Generic;
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
        private readonly Mock<IResourceRepository> _repo;
        private readonly Mock<IBatchJobClient> _batchClient;
        private readonly Mock<INotificationRepository> _notificationRepo;

        private readonly Resource _res1 = new Resource {Id = 1, Name = "John"};
        private readonly Resource _res2 = new Resource {Id = 2, Name = "Jane"};
        private readonly Resource _res3 = new Resource {Id = 3, Name = "Max"};

        public MapperJobTests()
        {
            _repo = new Mock<IResourceRepository>();
            _batchClient = new Mock<IBatchJobClient>();
            _notificationRepo = new Mock<INotificationRepository>();
        }

        [Fact]
        public void QueryIsCalledWithInitialState()
        {
            var initialPage = 1;

            var job = new DummyMapperJob(null, _repo.Object, null);

            job.Enqueue(new DummyMapperJob.State {Page = initialPage});

            _repo.Verify(r => r.List(initialPage), Times.Once);
        }

        [Fact]
        public void InitialBatchIsEnqueuedWithIndividualChildJobs()
        {
            var initialPage = 1;

            _repo.Setup(r => r.List(initialPage)).ReturnsAsync(new[] {_res1, _res2, _res3});

            Action<IBatchAction> action = null;

            _batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchStartedState>(), It.IsAny<string>()))
                .Callback<Action<IBatchAction>, IBatchState, string>((a, state, d) => action = a)
                .Returns("initial-batch-id");

            var job = new DummyMapperJob(_batchClient.Object, _repo.Object, null);

            job.Enqueue(new DummyMapperJob.State {Page = initialPage});

            var batchAction = new Mock<IBatchAction>();

            action(batchAction.Object);

            batchAction.Invocations.Should().SatisfyRespectively(
                jobA =>
                {
                    var enqueuedJob = jobA.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(_res1);
                    enqueuedJob.Method.Name.Should().Be("Next");

                    jobA.Arguments[1].Should().BeOfType<EnqueuedState>();
                },
                jobB =>
                {
                    var enqueuedJob = jobB.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(_res2);
                    enqueuedJob.Method.Name.Should().Be("Next");

                    jobB.Arguments[1].Should().BeOfType<EnqueuedState>();
                },
                jobC =>
                {
                    var enqueuedJob = jobC.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    enqueuedJob.Args[0].Should().BeEquivalentTo(_res3);
                    enqueuedJob.Method.Name.Should().Be("Next");

                    jobC.Arguments[1].Should().BeOfType<EnqueuedState>();
                });
        }

        [Fact]
        public void NextBatchIsEnqueued()
        {
            var initialState = new DummyMapperJob.State {Page = 1};
            var nextState = new DummyMapperJob.State {Page = 2};

            _repo.Setup(r => r.List(initialState.Page)).ReturnsAsync(new[] {_res1, _res2, _res3});

            var job = new DummyMapperJob(_batchClient.Object, _repo.Object, null);

            Action<IBatchAction> action = null;

            _batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchStartedState>(), It.IsAny<string>()))
                .Returns("initial-batch-id");

            _batchClient.Setup(
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
        public void OnJobStartedJobsAreScheduled()
        {
            var initialState = new DummyMapperJob.State {Page = 1};

            _repo.Setup(r => r.List(initialState.Page)).ReturnsAsync(new[] {_res1});

            Action<IBatchAction> action = null;

            _batchClient.Setup(
                    client => client.Create(
                        It.IsAny<Action<IBatchAction>>(), It.IsAny<BatchStartedState>(), It.IsAny<string>()))
                .Callback<Action<IBatchAction>, IBatchState, string>((a, state, d) => action = a)
                .Returns("initial-batch-id");

            var job = new DummyMapperJob(_batchClient.Object, _repo.Object, null);

            job.Enqueue(initialState);

            var batchAction = new Mock<IBatchAction>();

            action(batchAction.Object);

            batchAction.Invocations.Should().SatisfyRespectively(
                markJobAsStartedJob =>
                {
                    // TODO - Reuse
                    // TODO - test CallNotificationMethod 
                    var enqueuedJob = markJobAsStartedJob.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    markJobAsStartedJob.Arguments[1].Should().BeOfType<EnqueuedState>();

                    enqueuedJob.Args[0].Should().BeEquivalentTo("MarkJobAsStarted");
                    enqueuedJob.Args[1].Should().BeEquivalentTo(initialState);
                    enqueuedJob.Method.Name.Should().Be("CallNotificationMethod");
                },
                sendJobStartedEmailNotificationJob =>
                {
                    var enqueuedJob = sendJobStartedEmailNotificationJob.Arguments[0] as Job;

                    Assert.NotNull(enqueuedJob);

                    sendJobStartedEmailNotificationJob.Arguments[1].Should().BeOfType<EnqueuedState>();

                    enqueuedJob.Args[0].Should().BeEquivalentTo("SendJobStartedEmailNotification");
                    enqueuedJob.Args[1].Should().BeEquivalentTo(initialState);
                    enqueuedJob.Method.Name.Should().Be("CallNotificationMethod");
                },
                jobA => { });
        }
        
        // TODO - Test on started are scheduled only once
        // TODO - Test on completed are called once at the end 

        [Fact]
        public void NoJobsAreEnqueuedIfQueryReturnsNull()
        {
            _repo.Setup(r => r.List(It.IsAny<int>())).ReturnsAsync((IEnumerable<Resource>) null);

            var job = new DummyMapperJob(_batchClient.Object, _repo.Object, null);

            job.Enqueue(new DummyMapperJob.State());

            _batchClient.VerifyNoOtherCalls();
        }

        [Fact]
        public void NoJobsAreEnqueuedIfQueryReturnsEmptySet()
        {
            _repo.Setup(r => r.List(It.IsAny<int>())).ReturnsAsync(new List<Resource>());

            var job = new DummyMapperJob(_batchClient.Object, _repo.Object, null);

            job.Enqueue(new DummyMapperJob.State());

            _batchClient.VerifyNoOtherCalls();
        }
        
        // TODO - As an additional feature consider adding a context object that would context info about
        // the complete job, eg. total number of batches etc...
    }
}