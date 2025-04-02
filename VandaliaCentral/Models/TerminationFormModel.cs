using System.ComponentModel.DataAnnotations;

namespace VandaliaCentral.Models
{
    public class TerminationFormModel
    {
        [Required]
        public string EmployeeName { get; set; }

        [Required]
        public DateTime TerminationDate { get; set; }

        [Required]
        public string Reason { get; set; }
    }
}
