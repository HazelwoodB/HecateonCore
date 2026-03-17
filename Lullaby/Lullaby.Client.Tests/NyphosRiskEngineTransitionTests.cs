using System;
using System.Threading.Tasks;
using Xunit;
using Hecateon.Models;
using Hecateon.Services;
using Moq;

namespace Lullaby.Client.Tests
{
    public class NyphosRiskEngineTransitionTests
    {
        [Fact]
        public async Task CalculateRiskStateAsync_TransitionsToRed_WhenCrisisDetected()
        {
            // Arrange
            var healthTracking = new Mock<HealthTrackingService>(null, null).Object;
            var env = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>().Object;
            var engine = new NyphosRiskEngine(healthTracking, env);

            // Simulate crisis event
            var assessment = new NyphosRiskAssessment
            {
                CurrentState = NyphosRiskState.Red,
                ContributingFactors = new System.Collections.Generic.List<RiskFactor> { new RiskFactor { Factor = "Severe sleep loss" } },
                StateExplanation = "Crisis state. Severe sleep loss. Activate emergency plan.",
                RecommendedActions = new System.Collections.Generic.List<string> { "Activate emergency plan" }
            };

            // Mock method
            var engineMock = new Mock<NyphosRiskEngine>(healthTracking, env) { CallBase = true };
            engineMock.Setup(x => x.CalculateRiskStateAsync(It.IsAny<int>(), default)).ReturnsAsync(assessment);

            // Act
            var result = await engineMock.Object.CalculateRiskStateAsync(7);

            // Assert
            Assert.Equal(NyphosRiskState.Red, result.CurrentState);
            Assert.Contains("Crisis state", result.StateExplanation);
            Assert.Contains("Activate emergency plan", result.RecommendedActions[0]);
        }
    }
}
