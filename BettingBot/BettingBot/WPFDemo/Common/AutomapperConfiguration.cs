using AutoMapper;
using WPFDemo.Models;

namespace WPFDemo.Common
{
    public static class AutoMapperConfiguration
    {
        public static IMapper Mapper { get; set; }

        public static void Configure()
        {
            var config = new MapperConfiguration(ConfigureUserMapping);
            Mapper = config.CreateMapper();
            AutoMapper.Mapper.Initialize(ConfigureUserMapping);
        }

        private static void ConfigureUserMapping(IProfileExpression cfg)
        {
            cfg.CreateMap<Bet, BetToDisplayVM>();
            cfg.CreateMap<Bet, BetToSendVM>();
            cfg.CreateMap<Tipster, TipsterForGvVM>();
            cfg.CreateMap<User, UserForGvVM>();
        }
    }
}
