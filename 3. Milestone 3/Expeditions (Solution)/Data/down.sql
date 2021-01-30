-- DOWN script for SQL Server

ALTER TABLE [Expedition] DROP CONSTRAINT [Expedition_FK_Peak];
ALTER TABLE [Expedition] DROP CONSTRAINT [Expedition_FK_TrekkingAgency];
ALTER TABLE [TeamMember] DROP CONSTRAINT [TeamMember_FK_Nation];
ALTER TABLE [ExpeditionMap] DROP CONSTRAINT [ExpeditionMap_FK_Expedition];
ALTER TABLE [ExpeditionMap] DROP CONSTRAINT [ExpeditionMap_FK_TeamMember];
ALTER TABLE [TeamMember] DROP CONSTRAINT [TeamMember_FK_Nation];

DROP TABLE [Expedition];
DROP TABLE [Peak];
DROP TABLE [TrekkingAgency];

DROP TABLE [NewsArticle];

DROP TABLE [ExpeditionMap];
DROP TABLE [TeamMember];
DROP TABLE [Nation];


