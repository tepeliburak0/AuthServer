using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.SharedLibrary.Dtos
{
    public class ErrorDto
    {
        public List<string> Errors { get;  set; }= new List<string>();
        public bool IsShow { get;  set; }
       
        public ErrorDto(string error,bool isShow)
        {
           
            Errors.Add(error);
            isShow = true;
        }
        public ErrorDto(List<string>errors,bool isShow)
        {
            Errors = Errors;
            IsShow = isShow;
        }
        
    }
}
