using Backend.Common.Interfaces.DataAccess;
using Backend.Entities;

namespace Backend.DataAccess
{
    public class DocumentRepository : Repository<Document>, IDocumentRepository
    {
        public DocumentRepository(DatabaseContext context) : base(context)
        {
        }
    }
}
