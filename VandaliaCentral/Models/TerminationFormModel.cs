using System.ComponentModel.DataAnnotations;

namespace VandaliaCentral.Models
{
    public class TerminationFormModel
    {
        [Required]
        public string EmployeeName { get; set; }

        [Required]
        public string EmployeeNumber { get; set; }

        [Required]
        public string Location { get; set; }

        [Required]
        public DateTime LastDateOfEmployment { get; set; }

        
        public bool Cellphone { get; set; }
        public bool Notepad { get; set; }
        public bool DeviceAccounts { get; set; }
        public bool Uniforms { get; set; }
        public bool CompanyVehicle { get; set; }
        public bool CreditCard { get; set; }
        public bool Laptop { get; set; }
        public bool Hotspot { get; set; }
        public bool DeviceLock { get; set; }
        public bool Keys { get; set; }
        public bool PromotionalItems { get;set; }
        public bool Microsoft365 { get; set; }
        public bool Vizion { get;set; }
        public bool Salesforce { get; set; }
        public bool Certify { get;set; }
        public bool RentalMan { get; set; }
        public bool Telematics { get;set; }
        public bool Paylocity { get; set; }
        public bool Ninety { get; set; }
        public bool Other { get; set; }
        public string OtherText { get;set; }


    }
}
