using System.ComponentModel.DataAnnotations;

namespace VandaliaCentral.Models
{
    public class ChangeFormModel
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public string PreviousJobTitle { get; set; } = string.Empty;

        [Required]
        public string BranchNumber { get; set; } = string.Empty;

        [Required]
        public string BranchName { get; set; } = string.Empty;

        [Required]
        public string ManagerName { get; set; } = string.Empty;

        [Required]
        public DateTime EffectiveDate { get; set; }

        [Required]
        public string NewPosition { get; set; } = string.Empty;

        [Required]
        public string JobTitle { get; set; } = string.Empty;

        public bool FullTime { get; set; }
        public bool PartTime { get; set; }
        public bool Hourly { get; set; }
        public bool Salary { get; set; }

        public string NewLocBranchNumber { get; set; } = string.Empty;

        public string NewLocBranchName { get; set; } = string.Empty;

        public string NewLocManagerName { get; set; } = string.Empty;

        public string CrossTrainingCurrentPosition { get; set; } = string.Empty;

        public string CrossTrainingTargetPosition { get; set; } = string.Empty;

        public string AdditionalNotes { get; set; } = string.Empty;
    }
}
