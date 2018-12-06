using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace test_cons
{
    public class Content
    {
        public int Id { get; set; }
        public string Eng { get; set; }
        public string Rus { get; set; }
        public int UserId { get; set; }
    }
}