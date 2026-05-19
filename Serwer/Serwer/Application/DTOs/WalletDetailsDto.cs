using System.Collections.Generic;

namespace Investe.Application.DTOs
{
    public class WalletDetailsDto : WalletDto
    {
        public List<PositionDto> Assets { get; set; } = new();
    }
}
