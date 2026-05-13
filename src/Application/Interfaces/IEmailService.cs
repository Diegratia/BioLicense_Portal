using System.Threading.Tasks;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, string? attachmentFileName = null, byte[]? attachmentData = null);
    }
}
