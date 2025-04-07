using System.ComponentModel.DataAnnotations;

namespace VandaliaCentral.Models
{
    public class ChangeFormModel
    {
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string PreviousJobTitle { get; set; }

        [Required]
        public string BranchNumber { get; set; }

        [Required]
        public string BranchName { get; set; }

        [Required]
        public string ManagerName { get; set; }

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string NewPosition { get; set; }

        [Required]
        public string JobTitle { get; set; }


        public bool FullTime { get; set; }
        public bool PartTime { get; set; }
        public bool Hourly { get; set; }
        public bool Salary { get; set; }

        public string NewLocBranchNumber { get; set; }
       
        public string NewLocBranchName { get; set; }

        public string NewLocManagerName { get; set; }

        public string AdditionalNotes { get; set; } 

    }
}
