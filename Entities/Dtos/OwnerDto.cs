using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Dtos
{
    public class OwnerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Address { get; set; }
    }
}
