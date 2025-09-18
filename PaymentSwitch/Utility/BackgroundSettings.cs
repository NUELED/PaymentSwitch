namespace PaymentSwitch.Utility
{
    public class BackgroundSettings
    {
        public int NibssTokenCacheTime { get; set; }
        public int FetchProcerssorInterval { get; set; }
        public int PushProcerssorInterval { get; set; }
        public int BATCH_SIZE { get; set; }
        public int MAX_SQL_PARAMS { get; set; }
        public int BankCode { get; set; }
    }
}
