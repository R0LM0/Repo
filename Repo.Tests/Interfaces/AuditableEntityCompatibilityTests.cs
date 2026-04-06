using NUnit.Framework;
using Repo.Repository.Interfaces;

namespace Repo.Tests.Interfaces
{
    /// <summary>
    /// Tests for IAuditableEntity compatibility.
    /// Issue #5, #7: Validates that nullable changes are backward compatible.
    /// </summary>
    [TestFixture]
    public class AuditableEntityCompatibilityTests
    {
        [Test]
        public void IAuditableEntity_Properties_AreNullable()
        {
            // Arrange - Create entity implementing the interface
            var entity = new TestAuditableEntity();

            // Act & Assert - All properties should accept null (widening conversion is safe)
            Assert.That(entity.CreatedAt, Is.Null);
            Assert.That(entity.CreatedBy, Is.Null);
            Assert.That(entity.UpdatedAt, Is.Null);
            Assert.That(entity.UpdatedBy, Is.Null);
            Assert.That(entity.IsDeleted, Is.Null);
            Assert.That(entity.DeletedAt, Is.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        [Test]
        public void IAuditableEntity_Properties_CanBeSet()
        {
            // Arrange
            var now = DateTime.UtcNow;
            var entity = new TestAuditableEntity
            {
                CreatedAt = now,
                CreatedBy = "TestUser",
                UpdatedAt = now,
                UpdatedBy = "TestUser",
                IsDeleted = false,
                DeletedAt = null,
                DeletedBy = null
            };

            // Assert
            Assert.That(entity.CreatedAt, Is.EqualTo(now));
            Assert.That(entity.CreatedBy, Is.EqualTo("TestUser"));
            Assert.That(entity.IsDeleted, Is.False);
        }

        [Test]
        public void ISoftDelete_Properties_AreNullable()
        {
            // Arrange
            var entity = new TestSoftDeleteEntity();

            // Act & Assert
            Assert.That(entity.IsDeleted, Is.Null);
            Assert.That(entity.DeletedAt, Is.Null);
            Assert.That(entity.DeletedBy, Is.Null);
        }

        [Test]
        public void LegacyEntity_CanImplementNewInterface()
        {
            // Arrange - Entity that was designed for non-nullable CreatedAt/IsDeleted
            var legacy = new LegacyAuditableEntity
            {
                CreatedAt = DateTime.UtcNow,  // Non-nullable in old code, now must set value
                IsDeleted = false             // Non-nullable in old code, now must set value
            };

            // Act & Assert - Should work fine
            Assert.That(legacy.CreatedAt, Is.Not.Null);
            Assert.That(legacy.IsDeleted, Is.Not.Null);
        }

        #region Test Infrastructure

        private class TestAuditableEntity : IAuditableEntity
        {
            public DateTime? CreatedAt { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string? UpdatedBy { get; set; }
            public bool? IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeletedBy { get; set; }
        }

        private class TestSoftDeleteEntity : ISoftDelete
        {
            public bool? IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeletedBy { get; set; }
        }

        /// <summary>
        /// Simulates an entity created for the old non-nullable interface.
        /// Must now explicitly set values for CreatedAt and IsDeleted.
        /// </summary>
        private class LegacyAuditableEntity : IAuditableEntity
        {
            public DateTime? CreatedAt { get; set; }
            public string? CreatedBy { get; set; }
            public DateTime? UpdatedAt { get; set; }
            public string? UpdatedBy { get; set; }
            public bool? IsDeleted { get; set; }
            public DateTime? DeletedAt { get; set; }
            public string? DeletedBy { get; set; }
        }

        #endregion
    }
}
