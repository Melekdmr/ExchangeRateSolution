using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRate.Core.Interfaces
{
    public interface IRepository <T> where T:IEntity
    {
        Task InsertAsync(T entity); 
        Task<List<T>> GetAllAsync();
        Task<T> GetByIdAsync(int id); 
        Task UpdateAsync(T entity); 
        Task DeleteAsync(int id);
    }
}
