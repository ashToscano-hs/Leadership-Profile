﻿using System;
using LeadershipProfileAPI.Data.Models;

namespace LeadershipProfileAPI.Features.Profile
{
    public class MappingProfile : AutoMapper.Profile
    {
        public MappingProfile()
        {
            CreateMap<ProfileList, List.TeacherProfile>()
                .ForMember(dst => dst.Id, opt => opt.MapFrom(x => Guid.NewGuid())) // Create Guid until Staff.Id is available from the vw_LeadershipProfileList
                .ForMember(dst => dst.FullName, opt => opt.MapFrom(x => GetFullName(x.FirstName, x.MiddleName, x.LastSurName)))
                .ForMember(dst => dst.YearsOfService, opt => opt.MapFrom(x => x.YearsOfService.HasValue ? decimal.ToInt32(x.YearsOfService.Value) : 0));

            CreateMap<ProfileHeader, Get.Response>()
                .ForMember(dst => dst.Certificates, opt => opt.Ignore())
                .ForMember(dst => dst.Education, opt => opt.Ignore())
                .ForMember(dst => dst.PositionHistory, opt => opt.Ignore())
                .ForMember(dst => dst.ProfessionalDevelopment, opt => opt.Ignore())
                .ForMember(dst => dst.InterestedInNextRole, opt => opt.MapFrom(x => false))
                .ForMember(dst => dst.LastName, opt => opt.MapFrom(x => x.LastSurname))
                .ForMember(dst => dst.FullName, opt => opt.MapFrom(x => GetFullName(x.FirstName, x.MiddleName, x.LastSurname)))
                .ForMember(dst => dst.District, opt => opt.MapFrom(x => x.Location))
                .ForMember(dst => dst.Phone, opt => opt.MapFrom(x => x.Telephone))
                .ForMember(dst => dst.StartDate, opt => opt.Ignore())
                .ForMember(dst => dst.CurrentPosition, opt => opt.MapFrom(x => x.Position));

            CreateMap<ProfilePositionHistory, Get.PositionHistory>()
                .ForMember(dst => dst.SchoolName, opt => opt.MapFrom(x => x.School));

            CreateMap<ProfileCertification, Get.Certificate>()
                .ForMember(dst => dst.Type, opt => opt.MapFrom(x => x.CredentialType))
                .ForMember(dst => dst.ValidFromDate, opt => opt.MapFrom(x => x.IssuanceDate))
                .ForMember(dst => dst.ValidToDate, opt => opt.MapFrom(x => x.ExpirationDate));

            CreateMap<StaffEducation, Get.TeacherEducation>()
                .ForMember(dst => dst.Institution, opt => opt.MapFrom(x => x.InstitutionAttended))
                .ForMember(dst => dst.Degree, opt => opt.MapFrom(x => x.DegreeAwarded))
                .ForMember(dst => dst.Specialization, opt => opt.MapFrom(x => x.MajorOrSpecialization));

            CreateMap<StaffProfessionalDevelopment, Get.ProfessionalDevelopment>()
                .ForMember(dst => dst.AttendanceDate, opt => opt.MapFrom(x => x.AttendanceDate))
                .ForMember(dst => dst.ProfessionalDevelopmentTitle, opt => opt.MapFrom(x => x.ProfessionalDevelopmentTitle))
                .ForMember(dst => dst.Location, opt => opt.MapFrom(x => x.Location))
                .ForMember(dst => dst.AlignmentToLeadership, opt => opt.MapFrom(x => x.AlignmentToLeadership));
        }

        private static string GetFullName(string firstName, string middleName, string lastName)
        {
            var fullName = firstName;

            if (!string.IsNullOrWhiteSpace(middleName))
            {
                fullName += " " + middleName;
            }

            fullName += " " + lastName;

            return fullName;
        }
    }
}
