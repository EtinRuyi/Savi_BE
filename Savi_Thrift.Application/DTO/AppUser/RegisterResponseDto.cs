﻿namespace Savi_Thrift.Application.DTO.AppUser
{
    public class RegisterResponseDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Token { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLogin { get; set; }
        public string IsDeleted { get; set; }
        public string ImageUrl { get; set; }
    }
}
