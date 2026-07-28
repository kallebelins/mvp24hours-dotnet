using CustomerAPI.Application.DTOs.ExternalProfiles;
using CustomerAPI.Application.Ports;
using CustomerAPI.Core.Ports;
using CustomerAPI.Core.Resources;
using Microsoft.Extensions.Logging;
using Mvp24Hours.Core.Contract.ValueObjects.Logic;
using Mvp24Hours.Core.Enums;
using Mvp24Hours.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CustomerAPI.Application.UseCases
{
    /// <summary>
    /// Application use case that enriches responses with external profile data
    /// fetched through the <see cref="IExternalProfilePort"/> outbound port.
    /// </summary>
    public class ExternalProfileUseCase(
        IExternalProfilePort externalProfilePort,
        ILogger<ExternalProfileUseCase> logger) : IExternalProfileUseCase
    {
        public async Task<IBusinessResult<IList<ExternalProfileResult>>> GetProfilesAsync(CancellationToken cancellationToken = default)
        {
            logger.LogDebug("GetProfilesAsync called — fetching from external profile port");

            var profiles = await externalProfilePort.GetProfilesAsync(cancellationToken);

            if (!profiles.AnySafe())
                return Messages.RECORD_NOT_FOUND
                    .ToMessageResult(nameof(Messages.RECORD_NOT_FOUND), MessageType.Error)
                    .ToBusiness<IList<ExternalProfileResult>>();

            IList<ExternalProfileResult> result = profiles.Select(p => new ExternalProfileResult
            {
                Id = p.Id,
                Name = p.Name,
                Username = p.Username,
                Email = p.Email,
                Phone = p.Phone,
                Website = p.Website
            }).ToList();

            return result.ToBusiness();
        }

        public async Task<IBusinessResult<ExternalProfileResult>> GetProfileByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            logger.LogDebug("GetProfileByIdAsync called with id={Id}", id);

            var profile = await externalProfilePort.GetProfileByIdAsync(id, cancellationToken);

            if (profile == null)
                return Messages.EXTERNAL_PROFILE_NOT_FOUND
                    .ToMessageResult(nameof(Messages.EXTERNAL_PROFILE_NOT_FOUND), MessageType.Error)
                    .ToBusiness<ExternalProfileResult>();

            var result = new ExternalProfileResult
            {
                Id = profile.Id,
                Name = profile.Name,
                Username = profile.Username,
                Email = profile.Email,
                Phone = profile.Phone,
                Website = profile.Website
            };

            return result.ToBusiness();
        }
    }
}
