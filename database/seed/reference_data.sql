SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;

INSERT INTO dbo.FiscalYearType (FiscalYearCode, Description)
VALUES ('0112', N'January - December'),
       ('0403', N'April - March'),
       ('0706', N'July - June'),
       ('1009', N'October - September');

INSERT INTO dbo.Nationality (NationalityName)
VALUES (N'Greek'), (N'Filipino'), (N'Indian'), (N'Indonesian'), (N'Chinese'), (N'Ukrainian'),
       (N'Croatian'), (N'Mexican'), (N'Burmese'), (N'Vietnamese'), (N'Polish'), (N'Romanian');

INSERT INTO dbo.[Rank] (RankCode, RankName, Department, SeniorityOrder)
VALUES ('MSTR', N'Master',                   'Deck',      1),
       ('CE',   N'Chief Engineer',           'Engine',    2),
       ('CO',   N'Chief Officer',            'Deck',      3),
       ('2E',   N'Second Engineer',          'Engine',    4),
       ('2O',   N'Second Officer',           'Deck',      5),
       ('3E',   N'Third Engineer',           'Engine',    6),
       ('3O',   N'Third Officer',            'Deck',      7),
       ('4E',   N'Fourth Engineer',          'Engine',    8),
       ('ETO',  N'Electro-Technical Officer','Engine',    9),
       ('BSN',  N'Bosun',                    'Deck',     10),
       ('FTR',  N'Fitter',                   'Engine',   11),
       ('AB',   N'Able Seaman',              'Deck',     12),
       ('OLR',  N'Oiler',                    'Engine',   13),
       ('OS',   N'Ordinary Seaman',          'Deck',     14),
       ('WPR',  N'Wiper',                    'Engine',   15),
       ('CCK',  N'Chief Cook',               'Catering', 16),
       ('MSM',  N'Messman',                  'Catering', 17),
       ('DCDT', N'Deck Cadet',               'Deck',     18),
       ('ECDT', N'Engine Cadet',             'Engine',   19);

INSERT INTO dbo.UserRole (RoleName)
VALUES ('Administrator'), ('Fleet Manager'), ('Superintendent'), ('Accountant'), ('Viewer');

INSERT INTO dbo.Account (AccountNumber, AccountName, AccountType, ParentAccountNumber)
VALUES ('7000000', N'OPERATING EXPENSES',                   'P', NULL);

INSERT INTO dbo.Account (AccountNumber, AccountName, AccountType, ParentAccountNumber)
VALUES ('7100000', N'AWARD AND GRANT TO INDIVIDUALS',       'P', '7000000'),
       ('7200000', N'CREW COSTS',                           'P', '7000000'),
       ('7300000', N'STORES AND SPARES',                    'P', '7000000'),
       ('7400000', N'LUBRICANTS',                           'P', '7000000'),
       ('7500000', N'REPAIRS AND MAINTENANCE',              'P', '7000000'),
       ('7600000', N'INSURANCE',                            'P', '7000000'),
       ('7700000', N'MANAGEMENT AND ADMINISTRATION',        'P', '7000000');

INSERT INTO dbo.Account (AccountNumber, AccountName, AccountType, ParentAccountNumber)
VALUES ('7110000', N'CREW WELFARE GRANTS',                  'C', '7100000'),
       ('7120000', N'AWARDS',                               'C', '7100000'),
       ('7125000', N'LONG SERVICE BONUSES',                 'C', '7100000'),
       ('7130000', N'TRAINING GRANTS',                      'C', '7100000'),
       ('7135000', N'SCHOLARSHIPS',                         'C', '7100000'),

       ('7210000', N'CREW WAGES',                           'C', '7200000'),
       ('7220000', N'CREW OVERTIME',                        'C', '7200000'),
       ('7230000', N'CREW TRAVEL',                          'C', '7200000'),
       ('7240000', N'CREW VICTUALLING',                     'C', '7200000'),
       ('7250000', N'CREW MEDICAL',                         'C', '7200000'),
       ('7260000', N'CREW TRAINING',                        'C', '7200000'),

       ('7310000', N'DECK STORES',                          'C', '7300000'),
       ('7320000', N'ENGINE STORES',                        'C', '7300000'),
       ('7330000', N'CABIN STORES',                         'C', '7300000'),
       ('7340000', N'DECK SPARES',                          'C', '7300000'),
       ('7350000', N'ENGINE SPARES',                        'C', '7300000'),

       ('7410000', N'MAIN ENGINE LUBE OIL',                 'C', '7400000'),
       ('7420000', N'AUXILIARY ENGINE LUBE OIL',            'C', '7400000'),
       ('7430000', N'CYLINDER OIL',                         'C', '7400000'),
       ('7440000', N'HYDRAULIC OIL',                        'C', '7400000'),
       ('7450000', N'GREASES',                              'C', '7400000'),

       ('7510000', N'HULL REPAIRS',                         'C', '7500000'),
       ('7520000', N'MACHINERY REPAIRS',                    'C', '7500000'),
       ('7530000', N'ELECTRICAL REPAIRS',                   'C', '7500000'),
       ('7540000', N'DRY DOCKING',                          'C', '7500000'),
       ('7550000', N'SURVEYS AND CLASS FEES',               'C', '7500000'),

       ('7610000', N'HULL AND MACHINERY INSURANCE',         'C', '7600000'),
       ('7620000', N'P AND I INSURANCE',                    'C', '7600000'),
       ('7630000', N'WAR RISK INSURANCE',                   'C', '7600000'),
       ('7640000', N'LOSS OF HIRE INSURANCE',               'C', '7600000'),
       ('7650000', N'FREIGHT DEMURRAGE AND DEFENCE',        'C', '7600000'),

       ('7710000', N'MANAGEMENT FEE',                       'C', '7700000'),
       ('7720000', N'COMMUNICATIONS',                       'C', '7700000'),
       ('7730000', N'BANK CHARGES',                         'C', '7700000'),
       ('7740000', N'AGENCY FEES',                          'C', '7700000'),
       ('7750000', N'SUNDRIES',                             'C', '7700000');

COMMIT TRANSACTION;
GO
