using NUnit.Framework;
using Repo.Repository.Models;

namespace Repo.Tests.Models
{
    /// <summary>
    /// Tests for PagedResult backward compatibility.
    /// Issue #5, #7: Validates that breaking changes are mitigated with shim properties.
    /// </summary>
    [TestFixture]
    public class PagedResultCompatibilityTests
    {
        [Test]
        public void PagedResult_DataProperty_Works()
        {
            var data = new List<string> { "item1", "item2" };
            var result = new PagedResult<string>
            {
                Data = data,
                TotalCount = 10,
                Page = 2,
                PageSize = 5,
                TotalPages = 2
            };

            Assert.That(result.Data, Is.EqualTo(data));
            Assert.That(result.Page, Is.EqualTo(2));
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_ItemsProperty_MapsToData()
        {
            var data = new List<string> { "item1", "item2" };
            var result = new PagedResult<string>
            {
                Data = data
            };

            // Items should return the same data
            Assert.That(result.Items, Is.EqualTo(data));
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_ItemsSetter_UpdatesData()
        {
            var items = new List<string> { "item1", "item2" };
            var result = new PagedResult<string>();

            result.Items = items;

            Assert.That(result.Data, Is.EqualTo(items));
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_PageNumberProperty_MapsToPage()
        {
            var result = new PagedResult<string>
            {
                Page = 3
            };

            Assert.That(result.PageNumber, Is.EqualTo(3));
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_PageNumberSetter_UpdatesPage()
        {
            var result = new PagedResult<string>();

            result.PageNumber = 5;

            Assert.That(result.Page, Is.EqualTo(5));
        }

        [Test]
        public void PagedResult_Constructor_SetsProperties()
        {
            var items = new[] { "a", "b", "c" };
            var result = new PagedResult<string>(items, 100, 2, 10);

            Assert.That(result.Data, Is.EqualTo(items));
            Assert.That(result.TotalCount, Is.EqualTo(100));
            Assert.That(result.Page, Is.EqualTo(2));
            Assert.That(result.PageSize, Is.EqualTo(10));
            Assert.That(result.TotalPages, Is.EqualTo(10));
        }

        [Test]
        public void PagedResult_HasPreviousPage_CalculatesCorrectly()
        {
            var result1 = new PagedResult<string> { Page = 1 };
            var result2 = new PagedResult<string> { Page = 2 };

            Assert.That(result1.HasPreviousPage, Is.False);
            Assert.That(result2.HasPreviousPage, Is.True);
        }

        [Test]
        public void PagedResult_HasNextPage_CalculatesCorrectly()
        {
            var result1 = new PagedResult<string> { Page = 1, TotalPages = 1 };
            var result2 = new PagedResult<string> { Page = 1, TotalPages = 2 };

            Assert.That(result1.HasNextPage, Is.False);
            Assert.That(result2.HasNextPage, Is.True);
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_BidirectionalMapping_ItemsAndData()
        {
            var result = new PagedResult<int>();

            // Set via Items
            result.Items = new[] { 1, 2, 3 };
            Assert.That(result.Data, Is.EqualTo(new[] { 1, 2, 3 }));

            // Modify Data
            result.Data = new List<int> { 4, 5 };
            Assert.That(result.Items, Is.EqualTo(new[] { 4, 5 }));
        }

        [Test]
        [Obsolete("Testing obsolete property for backward compatibility")]
        public void PagedResult_BidirectionalMapping_PageNumberAndPage()
        {
            var result = new PagedResult<string>();

            // Set via PageNumber
            result.PageNumber = 10;
            Assert.That(result.Page, Is.EqualTo(10));

            // Modify Page
            result.Page = 20;
            Assert.That(result.PageNumber, Is.EqualTo(20));
        }
    }
}
