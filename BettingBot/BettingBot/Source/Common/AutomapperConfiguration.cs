using AutoMapper;
using BettingBot.Source.DbContext.Models;

namespace BettingBot.Source.Common
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
            const int maxDepth = 1;
            cfg.CreateMap<DbBet, DbBet>().MaxDepth(maxDepth);
            cfg.CreateMap<DbDiscipline, DbDiscipline>().MaxDepth(maxDepth);
            cfg.CreateMap<DbLeague, DbLeague>().MaxDepth(maxDepth);
            cfg.CreateMap<DbLeagueAlternateName, DbLeagueAlternateName>().MaxDepth(maxDepth);
            cfg.CreateMap<DbLogin, DbLogin>().MaxDepth(maxDepth);
            cfg.CreateMap<DbMatch, DbMatch>().MaxDepth(maxDepth);
            cfg.CreateMap<DbOption, DbOption>().MaxDepth(maxDepth);
            cfg.CreateMap<DbPick, DbPick>().MaxDepth(maxDepth);
            cfg.CreateMap<DbTeam, DbTeam>().MaxDepth(maxDepth);
            cfg.CreateMap<DbTeamAlternateName, DbTeamAlternateName>().MaxDepth(maxDepth);
            cfg.CreateMap<DbTipster, DbTipster>().MaxDepth(maxDepth);
            cfg.CreateMap<DbWebsite, DbWebsite>().MaxDepth(maxDepth);
        }
    }
}
