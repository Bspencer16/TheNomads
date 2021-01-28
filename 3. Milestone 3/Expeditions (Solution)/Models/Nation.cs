using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace Expeditions.Models
{   
    [Table("Nation")]
    public partial class Nation
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]

        public string Name { get; set; }

    }
}
