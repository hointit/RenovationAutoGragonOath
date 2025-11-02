/*==============================================================*/
/* Database name:  tlbbdb                                       */
/* DBMS name:      MySQL 3.23                                   */
/* Created on:     2007-7-20 15:07:53                           */
/*==============================================================*/

create database if not exists tlbbdb;
use tlbbdb;

/*==============================================================*/
/* Database: tlbbdb                                             */
/*==============================================================*/
create database if not exists tlbbdb;

use tlbbdb;

/*==============================================================*/
/* Table: t_ability                                             */
/*==============================================================*/
create table if not exists t_ability
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   abid                           smallint                       not null,
   ablvl                          smallint                       not null,
   abexp                          int unsigned                   not null,
   dbversion                      int                            default 0,
   isvalid                        int                            default 1,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_ab_charguid                                     */
/*==============================================================*/
create index Index_ab_charguid on t_ability
(
   charguid
);

/*==============================================================*/
/* Table: t_char                                                */
/*==============================================================*/
create table if not exists t_char
(
   aid                            bigint                         not null AUTO_INCREMENT,
   accname                        varchar(50) binary             not null,
   charguid                       int                            not null,
   charname                       varchar(50) binary             not null,
   title                          varchar(50)                    not null,
   pw                             varchar(15)                    not null,
   sex                            smallint                       not null,
   level                          int                            not null,
   enegry                         int                            not null,
   energymax                      int                            not null,
   outlook                        int                            not null,
   scene                          int                            not null,
   xpos                           int                            not null,
   zpos                           int                            not null,
   menpai                         smallint                       not null,
   hp                             int                            not null,
   mp                             int                            not null,
   strikepoint                    smallint                       not null,
   str                            int                            not null,
   spr                            int                            not null,
   con                            int                            not null,
   ipr                            int                            not null,
   dex                            int                            not null,
   points                         int                            not null,
   logouttime                     int                            not null,
   logintime                      int                            not null,
   createtime                     int                            not null,
   haircolor                      int                            not null,
   hairmodel                      int                            not null,
   facecolor                      int                            not null,
   facemodel                      int                            not null,
   vmoney                         int                            not null,
   isvalid                        smallint                       not null,
   exp                            int                            not null,
   pres                           text                           not null,
   mdata                          text,
   mflag                          text,
   relflag                        text,
   settings                       text,
   dbversion                      int                            not null default 0,
   shopinfo                       text,
   carrypet                       varchar(20)                    not null,
   guldid                         int                            not null,
   teamid                         int                            not null,
   headid                         int                            not null,
   erecover                       int                            not null,
   vigor                          int                            not null,
   maxvigor                       int                            not null,
   vrecover                       int                            not null,
   pwdeltime                      int                            not null,
   pinfo                          text,
   bkscene                        int,
   bkxpos                         int,
   bkzpos                         int,
   titleinfo                      text,
   dietime                        int                            not null,
   cooldown                       text,
   bankmoney                      int                            not null,
   bankend                        int                            not null,
   rage                           int                            default 0,
   reserve                        varchar(100),
   dinfo                          int                            default 0,
   defeq                          int                            default -1,
   guildpoint                     int                            default 0,
   menpaipoint                    int                            default 0,
   gevil                          int                            default 0,
   pkvalue                        int                            default 0,
   otime                          int                            default 0,
   deltime                        int                            default 0,
   expinfo                        varchar(200)                   default '0',
   savetime                       int                            default 0,
   crc32                          int                            default 0,
   pvpinfo                        text                           default NULL,
   loginip                        int                            not null default 0,
   pkvaluetime                    int                            not null default 0,
   fatigue                        varchar(40)                    default NULL,
   yuanbao                        int                            not null default 0,
   visualgem                      text                           default NULL,
   isolditem                      smallint                       not null default 0,
   uipoint                        int                            not null default 0,
   zengdian                       int                            not null default 0,
   primary key (aid)
);

delimiter //
create procedure delete_char_new(
pcharname             varchar(100),
paccount              varchar(100),
pcharguid             int,
pdeltime              int,
pcrc32                int)
begin
declare rcharguid   int;
declare rlevel      int;
declare rdeltime    int;
declare rnext       int;
declare rdbversion  int;
declare rcrc        int;
set     rcharguid = -1;
set     rnext = 0;
set     rdbversion = 0;
start transaction;
select charguid,level,deltime,crc32 into rcharguid,rlevel,rdeltime,rcrc from t_char
    where accname=paccount and charguid = pcharguid and isvalid=1;
if rcharguid<>-1 then
	if rlevel<1 then
          update t_char set isvalid = 0,charname = CONCAT(charname,'@DELETE_',pcharguid)
            where accname= paccount and charguid = pcharguid;
          select dbversion into rdbversion from t_char
            where accname= paccount and charguid = pcharguid;
          set rnext = 1;
    else 
       set rcrc = rcrc + pcrc32;
	   if rdeltime=0 then
            update t_char set deltime= pdeltime,crc32=rcrc where charguid= pcharguid;        
	   end if;
	end if;
end if;
commit;
    select rnext,rdbversion;
end;//
delimiter ;

delimiter //
create procedure fetch_savetime(
pcharguid	int
)
begin
	declare 	visvalidtime 		 	int;
	declare 	vsavetime		 	int;
	declare 	vnowtime		 	 int;
    declare     vupdatetime          int;
	set 		vsavetime = -1;
	set 		vnowtime  = -1;
	set 		visvalidtime = 0;
 	start transaction;
	 select savetime into vsavetime from t_char where charguid = pcharguid;
	 if vsavetime = -1 then
		set  visvalidtime = 1;
	 else
          set vnowtime = time_to_sec(now());
	 	  if	abs(vsavetime-vnowtime)<300 then
	 	 	set  visvalidtime = 2;
		  else
	 	   
	 	  	update t_char set savetime = vnowtime where charguid = pcharguid;
	 	  	select savetime into vupdatetime from t_char where charguid = pcharguid;
	 	  	if vupdatetime = vnowtime then
	 	  		set visvalidtime = 3;
	 	  	else
	 	  	 	set visvalidtime = 4;
	 	  	end if;
	 	  end if;
	 end if;
  commit;
	select visvalidtime;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_char_charguid                                   */
/*==============================================================*/
create unique index Index_char_charguid on t_char
(
   charguid
);

/*==============================================================*/
/* Index: Index_char_accname                                    */
/*==============================================================*/
create index Index_char_accname on t_char
(
   accname
);

/*==============================================================*/
/* Index: Index_char_charname                                   */
/*==============================================================*/
create unique index Index_char_charname on t_char
(
   charname
);

/*==============================================================*/
/* Index: Index_char_level                                      */
/*==============================================================*/
create index Index_char_level on t_char
(
   level,
   exp
);

/*==============================================================*/
/* Index: Index_char_yuanbao                                    */
/*==============================================================*/
create index Index_char_yuanbao on t_char
(
   yuanbao
);

/* Table: t_charextra                                           */
/*==============================================================*/
create table if not exists t_charextra
(
   charguid                       int                            not null,
   dbversion                      int                            not null default 0,
   buyyuanbao                     int                            not null default 0,
   kmcount                        int                            not null default 0,
   cmcount                        tinyint                        not null default 0,
   sbmoney                        int unsigned                   not null default 0,
   sbunlock                       int unsigned                   not null default 0,
   sbstatus                       smallint                       not null default 0,
   ipregion                       int                            not null default -1,
   primary key (charguid)
);

delimiter //
create procedure save_charextra(
pcharguid           int,
pdbversion          int,
pbuyyuanbao         int,
pkmcount            int,
pcmcount            tinyint,
psbmoney            int unsigned,
psbunlock           int unsigned,
psbstatus           smallint,
pipregion           int
)
begin
	declare sameid  int;
    set sameid = -1;
    
    select charguid into sameid from t_charextra where charguid=pcharguid;
    if  sameid <> -1 then
        update t_charextra set
          dbversion=pdbversion,
          buyyuanbao=pbuyyuanbao,
          kmcount=pkmcount,
          cmcount=pcmcount,
          sbmoney=psbmoney,
          sbunlock=psbunlock,
          sbstatus=psbstatus,
          ipregion=pipregion
        where charguid=pcharguid;
    else
        insert into t_charextra (
          charguid,
          dbversion,
          buyyuanbao,
          kmcount,
          cmcount,
          sbmoney,
          sbunlock,
          sbstatus,
          ipregion)
        values (
          pcharguid,
          pdbversion,
          pbuyyuanbao,
          pkmcount,
          pcmcount,
          psbmoney,
          psbunlock,
          psbstatus,
          pipregion);
    end if;
end;//
delimiter ;


/*==============================================================*/
/* Table: t_city                                                */
/*==============================================================*/
create table if not exists t_city
(
   aid                            bigint                         not null AUTO_INCREMENT,
   poolid                         int                            not null,
   citydata                       longtext                       not null,
   isvalid                        int                            not null,
   primary key (aid)
);

delimiter //
create procedure save_cityinfo(pcitydata longtext,ppoolid int,pisvalid int)
begin
declare rcount int;
start transaction;
select poolid into rcount from t_city where poolid = ppoolid;
if rcount = ppoolid then
		update t_city set citydata=pcitydata,isvalid=pisvalid
            	where poolid=ppoolid;
else
		insert into t_city(poolid,citydata,
        	isvalid)  values(ppoolid,pcitydata,pisvalid);	
end if;
commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_city_poolid                                     */
/*==============================================================*/
create index Index_city_poolid on t_city
(
   poolid
);

/*==============================================================*/
/* Table: t_crc32                                               */
/*==============================================================*/
create table if not exists t_crc32
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   logouttime                     int                            not null,
   crc32                          int                            not null,
   fulldata                       longtext                       not null,
   isdelete                       smallint                       not null default 0,
   server                         int                            not null,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_crc_charguid                                    */
/*==============================================================*/
create index Index_crc_charguid on t_crc32
(
   charguid
);

/*==============================================================*/
/* Table: t_cshop                                               */
/*==============================================================*/
create table if not exists t_cshop
(
   aid                            bigint                         not null AUTO_INCREMENT,
   worldid                        int                            not null,
   serverid                       int                            not null,
   poolid                         int                            not null,
   isvalid                        int                            not null default 0,
   cshopid                        int                            not null default -1,
   primary key (aid)
);

delimiter //
create procedure save_cshop(
pworldid            int,
pserverid           int,
ppoolid             int,
pcshopid            int
)
begin
	declare sameid  int;
    set sameid = -1;
    
start transaction;
    select poolid into sameid from t_cshop 
    where worldid=pworldid and serverid=pserverid and poolid=ppoolid;
    if  sameid <> -1 then
        update t_cshop set cshopid=pcshopid,isvalid=1
        where worldid=pworldid and serverid=pserverid and poolid=ppoolid;
    else
        insert into t_cshop (worldid,serverid,poolid,isvalid,cshopid)
        values (pworldid,pserverid,ppoolid, 1, pcshopid);
    end if;
commit;

end;//
delimiter ;


/*==============================================================*/
/* Index: Index_cshop_PoolId                                    */
/*==============================================================*/
create unique index Index_cshop_PoolId on t_cshop
(
   worldid,
   serverid,
   poolid
);

/*==============================================================*/
/* Table: t_cshopitem                                           */
/*==============================================================*/
create table if not exists t_cshopitem
(
   aid                            bigint                         not null AUTO_INCREMENT,
   cshopid                        int                            not null,
   cshoppos                       int                            not null,
   serial                         int                            not null default 0,
   charguid                       int                            not null,
   charname                       varchar(50) binary             not null,
   gtype                          smallint                       not null,
   gvalue                         int                            not null default 0,
   svalue                         int                            not null default 0,
   ctime                          int                            not null default 0,
   costctime                      int                            not null default 0,
   isvalid                        int                            not null default 0,
   primary key (aid)
);

delimiter //
create procedure save_cshopitem(
pcshopid            int,
pcshoppos           int,
pserial             int,
pcharguid           int,
pcharname           varchar(50) binary,
pgtype              smallint,
pgvalue             int,
psvalue             int,
pctime              int,
pcostctime          int
)
begin
	declare sameid  int;
    declare samepos int;
    set sameid = -1;
    set samepos = -1;

start transaction;
    select cshopid,cshoppos into sameid,samepos 
    from t_cshopitem 
    where cshopid=pcshopid and cshoppos=pcshoppos;
    
    if  sameid <> -1 then
        update t_cshopitem
        set serial=pserial,charguid=pcharguid,charname=pcharname,
            gtype=pgtype,gvalue=pgvalue,svalue=psvalue,ctime=pctime,
            costctime=pcostctime,isvalid=1
        where cshopid=pcshopid and cshoppos=pcshoppos;
    else
        insert into t_cshopitem (cshopid,cshoppos,serial,charguid,charname,gtype,gvalue,svalue,ctime,costctime,isvalid) 
        values (pcshopid,pcshoppos,pserial,pcharguid,pcharname,pgtype,pgvalue,psvalue,pctime,pcostctime,1);
    end if;
commit;

end;//
delimiter ;


/*==============================================================*/
/* Index: Index_cshopitem_cshopid                               */
/*==============================================================*/
create unique index Index_cshopitem_cshopid on t_cshopitem
(
   cshopid,
   cshoppos
);

/*==============================================================*/
/* Table: t_global                                              */
/*==============================================================*/
create table if not exists t_global
(
   poolid                         int                            not null,
   data1                          int                            not null,
   primary key (poolid)
);

delimiter //
create procedure save_global(
ppoolid             int,
pdata1              int
)
begin
	declare sameid  int;
    set sameid = -1;
    
    select poolid into sameid from t_global where poolid=ppoolid;
    if  sameid <> -1 then
        update t_global set data1=pdata1 where poolid=ppoolid;
    else
        insert into t_global (poolid,data1) values (ppoolid, pdata1);
    end if;
end;//
delimiter ;


/*==============================================================*/
/* Table: t_guild                                               */
/*==============================================================*/
create table if not exists t_guild
(
   aid                            bigint                         not null AUTO_INCREMENT,
   guildid                        int                            not null,
   guildname                      varchar(50)                    not null,
   guildstat                      int                            not null,
   chiefguid                      int                            not null,
   pcount                         int                            not null,
   ucount                         int                            not null,
   mucount                        int                            not null,
   gpoint                         int                            not null,
   guildmoney                     int                            not null,
   cityid                         int                            not null,
   time                         int                            not null,
   logevity                       int                            not null,
   contribu                       int                            not null,
   honor                          int                            not null,
   indlvl                         int                            not null,
   agrlvl                         int                            not null,
   comlvl                         int                            not null,
   deflvl                         int                            not null,
   techlvl                        int                            not null,
   ambilvl                        int                            not null,
   admin                          text                           not null,
   guilddesc                      varchar(150)                   not null,
   chiefname                      varchar(50)                    not null,
   cname                          varchar(50)                    not null,
   glvl                           int                            not null,
   isvalid                        int                            not null,
   guilduser                      text                           not null,
   guildmsg                       varchar(300)                   not null,
   guildextra                     longtext                       not null,
   primary key (aid)
);

delimiter //
create procedure save_guildinfo(
pguildid              int          ,
pguildname            varchar(50)  ,
pguildstat            int          ,
pchiefguid            int          ,
ppcount               int          ,
pucount               int          ,
pmucount              int          ,
pgpoint               int          ,
pguildmoney           int          ,
pcityid               int          ,
ptime                 int          ,
plogevity             int          ,
pcontribu             int          ,
phonor                int          ,
pindlvl               int          ,
pagrlvl               int          ,
pcomlvl               int          ,
pdeflvl               int          ,
ptechlvl              int          ,
pambilvl              int          ,
padmin                text 				 ,
pguilddesc            varchar(150) ,
pchiefname            varchar(50)  ,
pcname                varchar(50)  ,
pglvl                 int         ,
pguilduser            text        ,
pisvalid              int,
pguildmsg             varchar(300))
begin

declare rcount int;

start transaction;
select guildid into rcount from t_guild where guildid = pguildid;
if rcount = pguildid then
			update t_guild set guildid          =pguildid,   
          guildname        =pguildname ,
          guildstat        =pguildstat ,
          chiefguid        =pchiefguid ,
          pcount           =ppcount,    
          ucount           =pucount,    
          mucount          =pmucount,   
          gpoint           =pgpoint,    
          guildmoney       =pguildmoney,
          cityid           =pcityid,    
          time             =ptime,      
          logevity         =plogevity,  
          contribu         =pcontribu,  
          honor            =phonor,     
          indlvl           =pindlvl,    
          agrlvl           =pagrlvl,    
          comlvl           =pcomlvl,    
          deflvl           =pdeflvl,    
          techlvl          =ptechlvl,   
          ambilvl          =pambilvl,   
          admin            =padmin,     
          guilddesc        =pguilddesc, 
          chiefname        =pchiefname, 
          cname            =pcname,     
          glvl             =pglvl,      
          guilduser        =pguilduser,
          isvalid          =pisvalid,
          guildmsg         =pguildmsg 
          where guildid	   =pguildid;
else
	insert into t_guild(guildid,
                            guildname,   
                            guildstat,   
                            chiefguid,   
                            pcount,      
                            ucount,      
                            mucount,     
                            gpoint,      
                            guildmoney,  
                            cityid,      
                            time,        
                            logevity,    
                            contribu,    
                            honor,       
                            indlvl,      
                            agrlvl,      
                            comlvl,      
                            deflvl,      
                            techlvl,     
                            ambilvl,     
                            admin,       
                            guilddesc,   
                            chiefname,   
                            cname,       
                            glvl,        
                            guilduser,
                            isvalid,
                            guildmsg)    values
                            (pguildid,
                             pguildname, 
                             pguildstat, 
                             pchiefguid, 
                             ppcount,   
                             pucount,   
                             pmucount,  
                             pgpoint,   
                             pguildmoney,
                             pcityid,   
                             ptime,     
                             plogevity, 
                             pcontribu, 
                             phonor,    
                             pindlvl,   
                             pagrlvl,   
                             pcomlvl,   
                             pdeflvl,   
                             ptechlvl,  
                             pambilvl,  
                             padmin,    
                             pguilddesc,
                             pchiefname,
                             pcname,    
                             pglvl,     
                             pguilduser,
                             pisvalid,
                             pguildmsg) ;
end if;

commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_guild_gguild                                    */
/*==============================================================*/
create unique index Index_guild_gguild on t_guild
(
   guildid
);

/*==============================================================*/
/* Table: t_impact                                              */
/*==============================================================*/
create table if not exists t_impact
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   imdata                         text                           not null,
   dbversion                      int                            default 0,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_impact_charguid                                 */
/*==============================================================*/
create index Index_impact_charguid on t_impact
(
   charguid
);

/*==============================================================*/
/* Table: t_iteminfo                                            */
/*==============================================================*/
create table if not exists t_iteminfo
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   guid                           int                            not null,
   world                          int                            not null,
   server                         int                            not null,
   itemtype                       int                            not null,
   pos                            smallint                       not null,
   p1                             int                            not null,
   p2                             int                            not null,
   p3                             int                            not null,
   p4                             int                            not null,
   p5                             int                            not null,
   p6                             int                            not null,
   p7                             int                            not null,
   p8                             int                            not null,
   p9                             int                            not null,
   p10                            int                            not null,
   p11                            int                            not null,
   p12                            int                            not null,
   p13                            int                            not null,
   p14                            int                            not null,
   p15                            int                            default 0,
   p16                            int                            default 0,
   p17                            int                            default 0,
   creator                        varchar(40)                    default '0',
   isvalid                        smallint                       not null default 1,
   dbversion                      int                            default 0,
   fixattr                        varchar(200)                   not null,
   var                            varchar(40)                    default '0',
   visualid                       int                            not null default 0,
   maxgemid                       int                            not null default -1,
   primary key (aid)
);

delimiter //
create procedure save_iteminfo(
pcharguid             int,
pguid                 int,
pworld                int,
pserver               int,
pitemtype             int,
ppos                  smallint,
pvisualid             int,
pmaxgemid             int,
pfixattr              varchar(100),
pp1                   int,
pp2                   int,
pp3                   int,
pp4                   int,
pp5                   int,
pp6                   int,
pp7                   int,
pp8                   int,
pp9                   int,
pp10                  int,
pp11                  int,
pp12                  int,
pp13                  int,
pp14                  int,
pp15                  int,
pp16                  int,
pp17                  int,
pisvalid              smallint,
pdbversion            int,
pcreator              varchar(40),
pvar                  varchar(40))
begin

declare rguid      int;
declare rpos	   int;

start transaction;
select charguid,pos into rguid,rpos from t_iteminfo
	where charguid = pcharguid and pos=ppos;
	if rguid = pcharguid then
	 update t_iteminfo set charguid = pcharguid,
		guid		 	= pguid,
		world		 	= pworld,
		server	 		= pserver,
		itemtype 		= pitemtype,
		pos			= ppos,
        visualid        = pvisualid,
        maxgemid        = pmaxgemid,
		fixattr	 		= pfixattr,
		p1			= pp1,
		p2			= pp2,
		p3			= pp3,
		p4			= pp4,
		p5			= pp5,
		p6			= pp6,
		p7			= pp7,
		p8			= pp8,
		p9			= pp9,
		p10			= pp10,
		p11			= pp11,
		p12			= pp12,
		p13			= pp13,
		p14			= pp14,
		p15			= pp15,
		p16			= pp16,
		p17			= pp17,
		isvalid  		= pisvalid,
		dbversion		= pdbversion,
		creator  		= pcreator,
		var			= pvar where
		charguid=pcharguid and pos=ppos and dbversion<=pdbversion;
      
      if row_count() > 0 then
        update t_iteminfo set isvalid=0 
        where guid=pguid and world=pworld and server=pserver and charguid<>pcharguid and pos<>ppos;
      end if;
	else
		insert into t_iteminfo(charguid,world,server,guid,itemtype,
		pos,visualid,maxgemid,fixattr,p1,p2,p3,p4,p5,p6,p7,p8,p9,p10,p11,p12,p13,p14,p15,p16,p17,
		isvalid,dbversion,creator,var)
		values(pcharguid,pworld,pserver,pguid,pitemtype,ppos,pvisualid,pmaxgemid,
		pfixattr,pp1,pp2,pp3,pp4,pp5,pp6,pp7,pp8,pp9,pp10,
		pp11,pp12,pp13,pp14,pp15,pp16,pp17,pisvalid,pdbversion,pcreator,pvar);
	end if;
commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_it_charguid                                     */
/*==============================================================*/
create unique index Index_it_charguid on t_iteminfo
(
   charguid,
   pos
);

/*==============================================================*/
/* Index: Index_it_itemguid                                     */
/*==============================================================*/
create index Index_it_itemguid on t_iteminfo
(
   guid,
   world,
   server
);

/*==============================================================*/
/* Index: Index_iteminfo_itemtype                               */
/*==============================================================*/
create index Index_iteminfo_itemtype on t_iteminfo
(
   itemtype,
   isvalid
);

/*==============================================================*/
/* Table: t_itemkey                                             */
/*==============================================================*/
create table if not exists t_itemkey
(
   aid                            bigint                         not null AUTO_INCREMENT,
   sid                            int                            not null,
   smkey                          int                            not null,
   serial                         int                            not null,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_itk_sid                                         */
/*==============================================================*/
create index Index_itk_sid on t_itemkey
(
   sid
);

/*==============================================================*/
/* Table: t_mail                                                */
/*==============================================================*/
create table if not exists t_mail
(
   aid                            bigint                         not null AUTO_INCREMENT,
   sender                         varchar(50)                    not null,
   recer                          varchar(50)                    not null,
   mailinfo                       varchar(100)                   not null,
   mailcont                       text                           not null,
   pindex                         int                            not null,
   isvalid                        int                            not null default 0,
   primary key (aid)
);

delimiter //
create procedure save_mailinfo(p1 varchar(50),p2 varchar(50),p3 varchar(100),p4 varchar(300),p5 int,p6 int)
begin
declare rcount int;
start transaction;
select pindex into rcount from t_mail where pindex = p5;
if rcount = p5 then
		update t_mail set sender=p1,recer=p2,mailinfo=p3,
        	mailcont=p4,isvalid=p6 where pindex=p5;
else
		insert into t_mail(pindex,sender,recer,mailinfo,mailcont,isvalid)
		  values(p5,p1,p2,p3,p4,p6);
end if;
commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_mail_mail                                       */
/*==============================================================*/
create unique index Index_mail_mail on t_mail
(
   pindex
);

/*==============================================================*/
/* Table: t_mission                                             */
/*==============================================================*/
create table if not exists t_mission
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   missionid                      int                            not null,
   scriptid                       int                            not null,
   flag                           smallint                       not null,
   p1                             int                            not null,
   p2                             int                            not null,
   p3                             int                            not null,
   p4                             int                            not null,
   p5                             int                            not null,
   p6                             int                            not null,
   p7                             int                            not null,
   p8                             int                            not null,
   dbversion                      int                            default 0,
   isvalid                        int                            default 1,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_mi_charguid                                     */
/*==============================================================*/
create index Index_mi_charguid on t_mission
(
   charguid
);

/*==============================================================*/
/* Table: t_pet                                                 */
/*==============================================================*/
create table if not exists t_pet
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   hpetguid                       int                            not null,
   lpetguid                       int                            not null,
   dataxid                         int                            not null,
   petname                        varchar (50)                   not null,
   petnick                        varchar (50)                   not null,
   level                          int                            not null,
   needlevel                      int                            not null,
   atttype                        int                            not null,
   aitype                         int                            not null,
   camp                           varchar(30)                    not null,
   hp                             int                            not null,
   mp                             int                            not null,
   life                           int                            not null,
   pettype                        smallint                       not null,
   genera                         smallint                       not null,
   enjoy                          smallint                       not null,
   strper                         int                            not null,
   conper                         int                            not null,
   dexper                         int                            not null,
   sprper                         int                            not null,
   iprper                         int                            not null,
   gengu                          int                            not null,
   growrate                       int                            not null,
   repoint                        int                            not null,
   exp                            int                            not null,
   str                            int                            not null,
   con                            int                            not null,
   dex                            int                            not null,
   spr                            int                            not null,
   ipr                            int                            not null,
   skill                          varchar(50)                    not null,
   dbversion                      int                            default 0,
   flags                          int,
   isvalid                        int                            default 1,
   pwflag                         int                            default 0,
   pclvl                          int                            default 0,
   hspetguid                      int                            default 0,
   lspetguid                      int                            default 0,
   savvy                          int                            default 0,
   title                          varchar(200)                   not null default '',
   curtitle                       int                            not null default -1,
   us_unlock_time                 int                            not null default 0,
   us_reserve                     int                            not null default 0,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_Pet_Charguid                                    */
/*==============================================================*/
create index Index_Pet_Charguid on t_pet
(
   charguid
);

/*==============================================================*/
/* Table: t_petcreate                                           */
/*==============================================================*/
create table if not exists t_petcreate
(
   aid                            bigint                         not null AUTO_INCREMENT,
   pindex                         int                            not null,
   pairdata                       text                           not null,
   isvalid                        int                            not null,
   dataversion                    int                            not null default 0,
   primary key (aid)
);

delimiter //
create procedure save_petiteminfo(ppairdata text,ppoolid int,pisvalid int)
begin
declare rcount int;
start transaction;
select pindex into rcount from t_petcreate where pindex = ppoolid;
if rcount = ppoolid then
		update t_petcreate set pairdata=ppairdata,isvalid=pisvalid
            	where pindex=ppoolid;
else
		insert into t_petcreate(pindex,pairdata,isvalid)  
		values(ppoolid,ppairdata,pisvalid);	
end if;
commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_pcre_pindex                                     */
/*==============================================================*/
create index Index_pcre_pindex on t_petcreate
(
   pindex
);

/*==============================================================*/
/* Table: t_pshop                                               */
/*==============================================================*/
create table if not exists t_pshop
(
   aid                            bigint                         not null AUTO_INCREMENT,
   sid                            int                            not null,
   poolid                         int                            not null,
   shopguid                       varchar(50)                    not null,
   type                         int                            not null,
   stat                           int                            not null,
   maxbmoney                      int                            not null,
   basemoney                      int                            not null,
   createtime                     int                            not null,
   shopname                       varchar(50)                    not null,
   shopdesc                       varchar(50)                    not null,
   ownername                      varchar(50)                    not null,
   ownerguid                      int                            not null,
   isopen                         int                            not null,
   sale                           int                            not null,
   saleprice                      int                            not null,
   partner                        text                           not null,
   recoder                        longtext                       not null,
   stallinfo                      longtext                       not null,
   isvalid                        int                            not null,
   partnum                        int                            not null,
   subtype                        int                            not null,
   profit                         int                            not null,
   buyinfo                        longtext                       not null default '',
   dataversion                    int                            not null default 0,
   primary key (aid)
);

delimiter //
create procedure save_shopinfo(
psid                  int,
ppoolid               int,
pshopguid             varchar(50),
ptype                 int,
pstat                 int,
pmaxbmoney            int,
pbasemoney            int,
pprofit               int,
pcreatetime           int,
pshopname             varchar(50),
pshopdesc             varchar(50),
pownername            varchar(50),
pownerguid            int,
pisopen               int,
psale                 int,
psaleprice            int,
ppartner              text,
pisvalid              int,
ppartnum              int,
subtype               int)
begin
declare rcount int;
declare rindex int;
start transaction;
select sid ,poolid into rcount,rindex from t_pshop where sid = psid and poolid=ppoolid;
if rcount = psid then
		update t_pshop set  shopguid=pshopguid,
                            type=ptype,
                            stat=pstat,
                            maxbmoney=pmaxbmoney,
                            basemoney=pbasemoney,
                            profit   =pprofit,
                            createtime=pcreatetime,
                            shopname=pshopname,
                            shopdesc=pshopdesc,
                            ownername=pownername,
                            ownerguid=pownerguid,
                            isopen=pisopen,
                            sale=psale,
                            saleprice=psaleprice,
                            partner=ppartner,
                            isvalid=pisvalid,
                            partnum=ppartnum,
                            subtype=subtype 
                            where sid=psid and poolid=ppoolid;
else
		insert into t_pshop(sid,       
                            poolid,    
                            shopguid,  
                            type,      
                            stat,      
                            maxbmoney, 
                            basemoney, 
                            profit,
                            createtime,
                            shopname,  
                            shopdesc,  
                            ownername, 
                            ownerguid, 
                            isopen,    
                            sale,      
                            saleprice, 
                            partner,
                            recoder,
                            stallinfo,   
                            isvalid,   
                            partnum,
                            subtype)    values
                            (psid,       
                             ppoolid,    
                             pshopguid,  
                             ptype,      
                             pstat,      
                             pmaxbmoney, 
                             pbasemoney, 
                             pprofit,
                             pcreatetime,
                             pshopname,  
                             pshopdesc,  
                             pownername, 
                             pownerguid, 
                             pisopen,    
                             psale,      
                             psaleprice,
                             ppartner,
                             '',
                             '',   
                             pisvalid,   
                             ppartnum,
                             subtype); 
end if;
commit;
end;//
delimiter ;
/*==============================================================*/
/* Index: Index_pshop_shopguid                                  */
/*==============================================================*/
create index Index_pshop_shopguid on t_pshop
(
   shopguid
);

/*==============================================================*/
/* Index: Index_pshop_sidpid                                    */
/*==============================================================*/
create index Index_pshop_sidpid on t_pshop
(
   sid,
   poolid
);

/*==============================================================*/
/* Index: Index_pshop_ownerguid                                 */
/*==============================================================*/
create index Index_pshop_ownerguid on t_pshop
(
   ownerguid
);

/*==============================================================*/
/* Table: t_relation                                            */
/*==============================================================*/
create table if not exists t_relation
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   fguid                          int                            not null,
   fname                          varchar(50)                    not null,
   fpoint                         int                            not null,
   reflag                         smallint                       not null,
   groupid                        smallint                       not null,
   extdata                        varchar(100)                   not null,
   dbversion                      int                            default 0,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_re_charguid                                     */
/*==============================================================*/
create index Index_re_charguid on t_relation
(
   charguid
);

/*==============================================================*/
/* Table: t_skill                                               */
/*==============================================================*/
create table if not exists t_skill
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   skid                           smallint                       not null,
   sktime                         int,
   dbversion                      int                            default 0,
   isvalid                        int,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_sk_charguid                                     */
/*==============================================================*/
create index Index_sk_charguid on t_skill
(
   charguid
);

/*==============================================================*/
/* Table: t_var                                                 */
/*==============================================================*/
create table if not exists t_var
(
   maxcharguid                    int                            not null,
   primary key (maxcharguid)
);

delimiter //
create procedure fetch_guid()
begin
declare charguid  int default -1;
start transaction;
select t_var.maxcharguid into charguid from t_var limit 1; 
if charguid<>-1 then
    update t_var set t_var.maxcharguid = charguid+1 where t_var.maxcharguid=charguid;
end if;
commit;
select charguid;
end;//
delimiter ;

/*==============================================================*/
/* Table: t_xfallexp                                            */
/*==============================================================*/
create table if not exists t_xfallexp
(
   xflv                           int                            not null,
   id1all                         int                            not null,
   id2all                         int                            not null,
   id3all                         int                            not null,
   id4all                         int                            not null,
   id5all                         int                            not null,
   id6all                         int                            not null,
   id7all                         int                            not null,
   primary key (xflv)
);

/*==============================================================*/
/* Table: t_xinfa                                               */
/*==============================================================*/
create table if not exists t_xinfa
(
   aid                            bigint                         not null AUTO_INCREMENT,
   charguid                       int                            not null,
   xinfaid                        smallint                       not null,
   xinfalvl                       smallint                       not null,
   dbversion                      int                            default 0,
   primary key (aid)
);

/*==============================================================*/
/* Index: Index_xinfa_charguid                                  */
/*==============================================================*/
create index Index_xinfa_charguid on t_xinfa
(
   charguid
);



alter table t_ability   engine=innodb;
alter table t_char      engine=innodb;
alter table t_city      engine=innodb;
alter table t_guild     engine=innodb;
alter table t_impact    engine=innodb;
alter table t_iteminfo  engine=innodb;
alter table t_itemkey   engine=innodb;
alter table t_mail      engine=innodb;
alter table t_mission   engine=innodb;
alter table t_pet       engine=innodb;
alter table t_petcreate engine=innodb;
alter table t_pshop     engine=innodb;
alter table t_relation  engine=innodb;
alter table t_skill     engine=innodb;
alter table t_var       engine=innodb;
alter table t_xinfa     engine=innodb;

alter table t_xfallexp  engine=myisam;
alter table t_crc32     engine=myisam;
alter table t_global    engine=myisam;
alter table t_charextra engine=innodb;
alter table t_cshop     engine=innodb;
alter table t_cshopitem engine=innodb;

--1.top 50 level character
delimiter //
create procedure get_50level_list()
begin
 select accname,charguid,charname,menpai,level,exp 
 from t_char 
 where charname not like '%DELETE%'
 order by level desc, exp desc
 limit 50;
end//
delimiter ;

--2.top 50 xinfa character
delimiter //
create procedure get_50xinfa_list()
begin
  create temporary table tmp_xinfa
 (charguid int not null,
  totalexp bigint not null
 )engine=myisam;
 
 insert into tmp_xinfa
 select a.charguid,sum(
  case mod(a.xinfaid,6)
   when 1 then b.id1all
   when 2 then b.id2all
   when 3 then b.id3all
   when 4 then b.id4all
   when 5 then b.id5all
   when 0 then b.id6all
   else b.id7all
  end
  ) as totalexp
 from t_xinfa as a left join t_xfallexp as b
  on a.xinfalvl=b.xflv
 where a.charguid in(
 select charguid from t_char where charname not like '%DELETE%'
 )
 group by a.charguid
 order by totalexp desc
 limit 50;
 
 select a.accname,b.charguid,a.charname,a.menpai,a.exp,b.totalexp
 from t_char as a,tmp_xinfa as b 
 where b.charguid=a.charguid
 order by b.totalexp desc, a.exp desc;
 
end//
delimiter ;

--3.top 50 money character
delimiter //
create procedure get_50money_list()
begin
 select a.accname,a.charguid,a.charname,a.menpai, 
  (a.vmoney+a.bankmoney+if(isnull(b.profit),0,b.profit)) as totalmoney
 from t_char as a left join t_pshop as b
  on a.charguid=b.ownerguid 
 where a.charname not like '%DELETE%'
 order by totalmoney desc 
 limit 50;
end//
delimiter ;

--4.method of create character
delimiter //
create procedure create_newchar(
paccname              varchar(50) binary,
pcharname             varchar(50) binary,
psex                  smallint,
pcreatetime           int,
phaircolor            int,
phairmodel            int,
pfacecolor            int,
pfacemodel            int,
pheadid               int,
pdefeq                int)
begin

declare rguid      	  int default -1;
declare result		  int default -1;
start transaction; 
 select charguid into rguid from t_char where charname=pcharname limit 1;
 if found_rows() = 0 then
  set rguid = -1;
  update t_var set maxcharguid=maxcharguid+1;
  select maxcharguid into rguid from t_var limit 1;
  
  if rguid <> -1 then
   insert into t_char(accname,charguid,charname,title,pw,sex,level,enegry,outlook,scene,xpos,zpos,menpai,
    hp,mp,strikepoint,str,con,dex,spr,ipr,points,logouttime,logintime,createtime,dbversion,haircolor,
    hairmodel,facecolor,facemodel,vmoney,settings,isvalid,exp,pres,
    shopinfo,carrypet,guldid,teamid,headid,erecover,vigor,maxvigor,vrecover,energymax,pwdeltime,
    pinfo,bkscene,bkxpos,bkzpos,titleinfo,dietime,bankmoney,bankend,cooldown,defeq)
   values(paccname,rguid,pcharname,'','',psex,1,0,0,0,100,100,9,
    5000,5000,0,5,5,5,5,5,0,0,0,pcreatetime,0,phaircolor,
    phairmodel,pfacecolor,pfacemodel,0,'',1,0,'',
		'','',-1,-1,pheadid,0,0,0,0,0,0,
		'',0,0,0,'',0,0,20,'',pdefeq);
   select row_count() into result;
  else
   set result = -3; 
  end if;
 else
   set result = -2;	
 end if;
commit;
select result,rguid;
end//
delimiter ;

--5.method of cacultotal
delimiter //
create procedure cacultotal(
lowindex        int,
highindex       int)
begin

declare lowvalue  int;
declare highvalue int;

set lowvalue = 0;
set highvalue = 0;

select data1 into lowvalue from t_global where poolid=lowindex;
select data1 into highvalue from t_global where poolid=highindex;

select sum(highvalue*2000000000+lowvalue) as total;

end;//
delimiter ;
