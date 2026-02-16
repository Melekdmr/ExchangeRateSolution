using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExchangeRate.Core.Interfaces
{
  public interface IEntity
    {
        //tüm entitiylerin ortak alanı
        int Id { get; set; }
        DateTime CreatedAt { get; set; }
        DateTime UpdateAt { get; set; }
    }
}
