namespace AeShipManagement.Application.Common;

public static class UserIdRules
{
    public static void EnsureValid(int userId)
    {
        if (userId < 1)
        {
            throw new InvalidRequestException("User id must be 1 or greater.");
        }
    }
}
