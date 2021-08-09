namespace mifty
{
    public static class ResponseCode
    {
        public const byte Success = 0;
        public const byte FormatError = 1;
        public const byte ServerFailure = 2;
        public const byte NameError = 3;
        public const byte NotImplemented = 4;
        public const byte Refused = 5;
        public const byte YXDOMAIN = 6;
        public const byte YXRRSET = 7;
        public const byte NXRRSET = 8;
        public const byte NotAuthoritative = 9;
        public const byte NotZone = 10;
    }
}