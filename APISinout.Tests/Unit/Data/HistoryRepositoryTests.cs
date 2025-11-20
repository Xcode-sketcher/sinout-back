using Xunit;
using Moq;
using MongoDB.Driver;
using APISinout.Data;
using APISinout.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Linq.Expressions;

namespace APISinout.Tests.Unit.Data
{
    public class HistoryRepositoryTests
    {
        private readonly Mock<IMongoCollection<HistoryRecord>> _mockCollection;
        private readonly HistoryRepository _repository;

        public HistoryRepositoryTests()
        {
            _mockCollection = new Mock<IMongoCollection<HistoryRecord>>();
            _repository = new HistoryRepository(_mockCollection.Object);
        }

        [Fact]
        public async Task CreateRecordAsync_ShouldInsertRecord()
        {
            // Arrange - Configura registro para teste de inserção
            var record = new HistoryRecord { Id = "1", UserId = 1, DominantEmotion = "happy" };

            // Act
            await _repository.CreateRecordAsync(record);

            // Assert
            _mockCollection.Verify(c => c.InsertOneAsync(record, null, default), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnRecord_WhenRecordExists()
        {
            // Arrange - Configura mock para retornar registro existente
            var record = new HistoryRecord { Id = "1", UserId = 1 };
            var mockCursor = MockCursor(new List<HistoryRecord> { record });

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetByIdAsync("1");

            // Assert
            Assert.NotNull(result);
            Assert.Equal("1", result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenRecordDoesNotExist()
        {
            // Arrange - Configura mock para retornar lista vazia
            var mockCursor = MockCursor(new List<HistoryRecord>());

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetByIdAsync("999");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByUserIdAsync_ShouldReturnRecords()
        {
            // Arrange - Configura mock com registros para usuário específico
            var records = new List<HistoryRecord> 
            { 
                new HistoryRecord { Id = "1", UserId = 1, Timestamp = DateTime.UtcNow },
                new HistoryRecord { Id = "2", UserId = 1, Timestamp = DateTime.UtcNow.AddHours(-1) }
            };
            var mockCursor = MockCursor(records);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetByUserIdAsync(1);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task DeleteOldRecordsAsync_ShouldDeleteRecords()
        {
            // Arrange - Configura cenário para exclusão de registros antigos

            // Act
            await _repository.DeleteOldRecordsAsync(24);

            // Assert
            _mockCollection.Verify(c => c.DeleteManyAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                default), Times.Once);
        }

        [Fact]
        public async Task GetUserStatisticsAsync_ShouldReturnCorrectStatistics()
        {
            // Arrange - Configura mock com múltiplos registros para cálculo de estatísticas
            var records = new List<HistoryRecord> 
            { 
                new HistoryRecord 
                { 
                    Id = "1", 
                    UserId = 1, 
                    DominantEmotion = "happy", 
                    MessageTriggered = "Keep it up!",
                    Timestamp = DateTime.UtcNow,
                    EmotionsDetected = new Dictionary<string, double> { { "happy", 0.9 } }
                },
                new HistoryRecord 
                { 
                    Id = "2", 
                    UserId = 1, 
                    DominantEmotion = "sad", 
                    MessageTriggered = "Cheer up!",
                    Timestamp = DateTime.UtcNow.AddHours(-1),
                    EmotionsDetected = new Dictionary<string, double> { { "sad", 0.8 } }
                },
                new HistoryRecord 
                { 
                    Id = "3", 
                    UserId = 1, 
                    DominantEmotion = "happy", 
                    MessageTriggered = "Keep it up!",
                    Timestamp = DateTime.UtcNow.AddHours(-2),
                    EmotionsDetected = new Dictionary<string, double> { { "happy", 0.85 } }
                }
            };
            var mockCursor = MockCursor(records);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var stats = await _repository.GetUserStatisticsAsync(1);

            // Assert
            Assert.Equal(1, stats.PatientId);
            Assert.Equal(3, stats.TotalAnalyses);
            Assert.Equal("happy", stats.MostFrequentEmotion);
            Assert.Equal("Keep it up!", stats.MostFrequentMessage);
            Assert.Equal(2, stats.EmotionFrequency["happy"]);
            Assert.Equal(1, stats.EmotionFrequency["sad"]);
        }

        [Fact]
        public async Task GetByFilterAsync_ShouldReturnFilteredRecords()
        {
            // Arrange - Configura filtro para buscar registros por emoção dominante
            var filter = new HistoryFilter 
            { 
                PatientId = 1, 
                DominantEmotion = "happy",
                PageNumber = 1,
                PageSize = 10
            };

            var records = new List<HistoryRecord> 
            { 
                new HistoryRecord { Id = "1", UserId = 1, DominantEmotion = "happy" }
            };
            var mockCursor = MockCursor(records);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetByFilterAsync(filter);

            // Assert
            Assert.Single(result);
            Assert.Equal("happy", result[0]!.DominantEmotion!);
        }

        [Fact]
        public async Task GetByFilterAsync_ShouldApplyMultipleFilters()
        {
            // Arrange - Configura filtro múltiplo com data e mensagem
            var filter = new HistoryFilter 
            { 
                PatientId = 1, 
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow,
                HasMessage = true
            };

            var records = new List<HistoryRecord> 
            { 
                new HistoryRecord { Id = "1", UserId = 1, MessageTriggered = "Msg", Timestamp = DateTime.UtcNow.AddHours(-1) }
            };
            var mockCursor = MockCursor(records);

            _mockCollection.Setup(c => c.FindAsync(
                It.IsAny<FilterDefinition<HistoryRecord>>(),
                It.IsAny<FindOptions<HistoryRecord, HistoryRecord>>(),
                default))
                .ReturnsAsync(mockCursor.Object);

            // Act
            var result = await _repository.GetByFilterAsync(filter);

            // Assert
            Assert.Single(result);
        }

        private Mock<IAsyncCursor<T>> MockCursor<T>(List<T> items)
        {
            var mockCursor = new Mock<IAsyncCursor<T>>();
            mockCursor.Setup(_ => _.Current).Returns(items);
            
            mockCursor
                .SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                .Returns(true)
                .Returns(false);
                
            mockCursor
                .SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(true)
                .ReturnsAsync(false);

            return mockCursor;
        }
    }
}
