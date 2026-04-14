namespace WerkonWebServicesRatchet.Contracts.Clients;
    public sealed class SaveClientRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
