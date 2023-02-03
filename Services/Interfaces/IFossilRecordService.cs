using FossilRecordsProject.Models;

namespace FossilRecordsProject.Services.Interfaces
{
    public interface IFossilRecordService
    {
        public Task AddContactToCategoryAsync(int categoryId, int contactId);
        public Task AddContactToCategoriesAsync(IEnumerable<int> categoryIds, int contactId);
        public Task<IEnumerable<Category>> GetAppUserCategoriesAsync(string appUserId);
        public Task<bool> IsContactInCategory(int categoryId, int contactId);
        public Task RemoveAllContactCategoriesAsync(int contactId);
    }
}
