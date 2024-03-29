﻿using System.ComponentModel.DataAnnotations;

namespace FossilRecordsProject.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Category Name")]
        public string? Name { get; set; }

        [Required]
        public string? AppUserID { get; set; }

        // Navigation Property
        public virtual AppUser? AppUser { get; set; }
        public virtual ICollection<Contact> Contacts { get; set; } = new HashSet<Contact>();

    }
}
