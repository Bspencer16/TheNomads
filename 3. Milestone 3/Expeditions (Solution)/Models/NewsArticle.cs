using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expeditions.Models
{
    [Table("NewsArticle")]
    public partial class NewsArticle
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }
        [StringLength(100)]
        public string Title { get; set; }
        [StringLength(500)]
        public string Description { get; set; }
        [Column(TypeName = "date")]
        public DateTime? Date { get; set; }
    }
}
