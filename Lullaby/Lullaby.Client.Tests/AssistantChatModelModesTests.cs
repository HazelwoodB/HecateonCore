using System;
using System.Threading.Tasks;
using Xunit;
using Hecateon.Models;
using Hecateon.Services;
using Moq;

namespace Lullaby.Client.Tests
{
    public class AssistantChatModelModesTests
    {
        [Fact]
        public async Task ProcessUserMessageAsync_ReturnsCrisisTone_WhenRiskIsRed()
        {
            // Arrange
            var llmService = new Mock<LLMAssistantService>();
            llmService.Setup(x => x.GenerateReplyAsync(It.IsAny<string>(), default)).ReturnsAsync("Test reply");
            var sentimentModel = new Mock<SimpleSentimentModel>().Object;
            var chatLogService = new Mock<ChatLogService>().Object;
            var naturalnessEngine = new Mock<Hecateon.Client.Services.Foundation.ConversationalNaturalnessEngine>().Object;
            var preferenceManager = new Mock<Hecateon.Client.Services.Foundation.PreferenceManager>().Object;

            var model = new AssistantChatModel(
                llmService.Object,
                sentimentModel,
                chatLogService,
                naturalnessEngine,
                preferenceManager
            );

            // Simulate NyphosRiskAssessment with Red state
            var nyphosAssessment = new NyphosRiskAssessment
            {
                CurrentState = NyphosRiskState.Red,
                RiskScore = 95,
                ContributingFactors = new System.Collections.Generic.List<RiskFactor> { new RiskFactor { Factor = "Sleep deprivation" } },
                RecommendedActions = new System.Collections.Generic.List<string> { "Activate emergency plan" },
                StateExplanation = "Crisis state. Sleep deprivation. Activate emergency plan."
            };

            // Inject mock NyphosRiskEngine
            AppServices.RegisterInstance<Hecateon.Services.NyphosRiskEngine>(MockNyphosRiskEngine(nyphosAssessment));

            // Act
            var response = await model.ProcessUserMessageAsync("Help!", default);

            // Assert
            Assert.Contains("Crisis mode", response.Reply);
            Assert.Contains("Activate emergency plan", response.Reply);
        }

        private NyphosRiskEngine MockNyphosRiskEngine(NyphosRiskAssessment assessment)
        {
            var engine = new Mock<NyphosRiskEngine>(null, null) { CallBase = true };
            engine.Setup(x => x.CalculateRiskStateAsync(It.IsAny<int>(), default)).ReturnsAsync(assessment);
            return engine.Object;
        }
    }
}
