using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Expeditions.Models
{
    [Table("TeamMember")]
    public partial class TeamMember
    {
        [Key]
        [Column("ID")]
        public int Id { get; set; }

        [StringLength(30)]
        public string FirstName { get; set; }

        [StringLength(30)]
        public string LastName { get; set; }

        public int Age { get; set; }
    }

}
