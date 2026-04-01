namespace InteroperabiliteProject.Dtos
{


    public class PARAM_MESSAGE
    {
        public SMSDATA? sms { get; set; }
        public SMTPDATA? smtp { get; set; }
  
    }


    public class SMSDATA
    {
        public string? login { get; set; }
        public string? baseUri { get; set; } 
        public string? sender { get; set; }
        public string? text { get; set; }
        public string? pwd { get; set; }
        public int? validite_otp { get; set; } = 10;
       
    }

    public class SMTPDATA
    {
        public string? server { get; set; }
        public int port { get; set; } 
        public string? sender_name { get; set; }
        public string? sender_email { get; set; }
        public string? user { get; set; }
        public string? password { get; set; }
        public bool enable_ssl { get; set; }  = false;

    }
}
