using UrlShortener.BusinessLogic.DTOs;
using UrlShortener.DataAccess.Entities;

namespace UrlShortener.BusinessLogic.Mappers;

public static class UserMapper
{
    public static UserMinimalDto ToMinimalDto(this UserDbTable user)
    {
        return new UserMinimalDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Username = user.Username,
            Email = user.Email
        };
    }
}