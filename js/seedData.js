/**
 * M.J. Billones Transport Service - Initial Seed Data
 * Extracted directly from mj_billones.db SQLite database
 * Supports both standard script tags (file:// protocol) and ES6 imports
 */
window.INITIAL_SEED_DATA = {
  "Users": [
    {
      "Id": 1,
      "Username": "admin",
      "Password": "240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9",
      "Role": "Owner/Admin",
      "FullName": "Marie Ann J. Billones (Owner)",
      "ContactNumber": "0917-123-4567",
      "IsActive": 1,
      "CreatedAt": "2026-08-10 07:21:36",
      "SecurityQuestion": "mothers maiden name",
      "SecurityAnswer": "teresa",
      "LastName": "(Owner)",
      "FirstName": "Marie Ann J.",
      "MiddleName": "Billones",
      "PlainPassword": "admin123"
    },
    {
      "Id": 2,
      "Username": "staff",
      "Password": "staff123",
      "Role": "Office Staff/Dispatcher",
      "FullName": "Hazel Lagasca (Dispatcher)",
      "ContactNumber": "0918-987-6543",
      "IsActive": 1,
      "CreatedAt": "2026-08-10 07:21:36",
      "SecurityQuestion": "",
      "SecurityAnswer": "",
      "LastName": "(Dispatcher)",
      "FirstName": "Hazel",
      "MiddleName": "Lagasca",
      "PlainPassword": "staff123"
    },
    {
      "Id": 3,
      "Username": "HARMIN",
      "Password": "e98424fe3f2a09185e9078d00ce2adab90b88c06032abb9d77b0a3c9ba34e81d",
      "Role": "Office Staff",
      "FullName": "HARMIN DELA VASQUEZ",
      "ContactNumber": "9876755125",
      "IsActive": 0,
      "CreatedAt": "2026-09-04 22:27:04",
      "SecurityQuestion": "What is your mother's maiden name?",
      "SecurityAnswer": "mary",
      "LastName": "DELA VASQUEZ",
      "FirstName": "HARMIN",
      "MiddleName": ""
    }
  ],
  "Crews": [
    {
      "Id": 5,
      "FullName": "DEVELA, CARLO",
      "Role": "DRIVER",
      "ContactNumber": "09425144705",
      "LicenseNumber": "D01-24-000001",
      "DailyWage": 0.0,
      "FoodAllowance": 7.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "DEVELA",
      "FirstName": "CARLO",
      "MiddleName": ""
    },
    {
      "Id": 6,
      "FullName": "JIMENEZ, JESSIE A.",
      "Role": "Mechanic",
      "ContactNumber": "09425144706",
      "LicenseNumber": "A01-24-000001",
      "DailyWage": 1.0,
      "FoodAllowance": 1.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "JIMENEZ",
      "FirstName": "JESSIE",
      "MiddleName": "A."
    },
    {
      "Id": 7,
      "FullName": "MALLARI, JAY-AR",
      "Role": "Aide",
      "ContactNumber": "09425144707",
      "LicenseNumber": "A01-24-000002",
      "DailyWage": 0.0,
      "FoodAllowance": 0.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "MALLARI",
      "FirstName": "JAY-AR",
      "MiddleName": ""
    },
    {
      "Id": 8,
      "FullName": "MENDRANO, ROMEL",
      "Role": "Driver",
      "ContactNumber": "09425144708",
      "LicenseNumber": "D01-24-000002",
      "DailyWage": 0.0,
      "FoodAllowance": 0.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "MENDRANO",
      "FirstName": "ROMEL",
      "MiddleName": ""
    },
    {
      "Id": 9,
      "FullName": "MALLARI, JOHNATHAN",
      "Role": "Aide",
      "ContactNumber": "09425144709",
      "LicenseNumber": "A01-24-000003",
      "DailyWage": 0.0,
      "FoodAllowance": 0.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "MALLARI",
      "FirstName": "JOHNATHAN",
      "MiddleName": ""
    },
    {
      "Id": 10,
      "FullName": "PAGAYONAN, EDMON",
      "Role": "Driver",
      "ContactNumber": "09425144710",
      "LicenseNumber": "D01-24-000003",
      "DailyWage": 0.0,
      "FoodAllowance": 0.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "PAGAYONAN",
      "FirstName": "EDMON",
      "MiddleName": ""
    },
    {
      "Id": 11,
      "FullName": "PIAMONTE, CLEOFOLDO",
      "Role": "Driver",
      "ContactNumber": "09425144711",
      "LicenseNumber": "D01-24-000004",
      "DailyWage": 0.0,
      "FoodAllowance": 6.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "PIAMONTE",
      "FirstName": "CLEOFOLDO",
      "MiddleName": ""
    },
    {
      "Id": 12,
      "FullName": "RAMITTERE, GREGOREY",
      "Role": "Driver",
      "ContactNumber": "09425144712",
      "LicenseNumber": "D01-24-000005",
      "DailyWage": 0.0,
      "FoodAllowance": 6.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "RAMITTERE",
      "FirstName": "GREGOREY",
      "MiddleName": ""
    },
    {
      "Id": 13,
      "FullName": "TACUSALME RONALDO",
      "Role": "Aide",
      "ContactNumber": "09425144713",
      "LicenseNumber": "A01-24-000004",
      "DailyWage": 0.0,
      "FoodAllowance": 6.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "RONALDO",
      "FirstName": "TACUSALME",
      "MiddleName": ""
    },
    {
      "Id": 14,
      "FullName": "VALSOTE, KEN IVAN",
      "Role": "Aide",
      "ContactNumber": "09425144714",
      "LicenseNumber": "A01-24-000005",
      "DailyWage": 0.0,
      "FoodAllowance": 6.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "VALSOTE",
      "FirstName": "KEN",
      "MiddleName": "IVAN"
    },
    {
      "Id": 15,
      "FullName": "VERNANTE, ROMNICK",
      "Role": "Aide",
      "ContactNumber": "09425144715",
      "LicenseNumber": "A01-24-000006",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "VERNANTE",
      "FirstName": "ROMNICK",
      "MiddleName": ""
    },
    {
      "Id": 16,
      "FullName": "MARCILLANO, CATALINO",
      "Role": "Aide",
      "ContactNumber": "09425144716",
      "LicenseNumber": "A01-24-000007",
      "DailyWage": 3.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "MARCILLANO",
      "FirstName": "CATALINO",
      "MiddleName": ""
    },
    {
      "Id": 17,
      "FullName": "ROJO, MICHAEL",
      "Role": "Aide",
      "ContactNumber": "09425144717",
      "LicenseNumber": "A01-24-000008",
      "DailyWage": 3.0,
      "FoodAllowance": 18.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "ROJO",
      "FirstName": "MICHAEL",
      "MiddleName": ""
    },
    {
      "Id": 18,
      "FullName": "DO-OMA, REGEI",
      "Role": "Aide",
      "ContactNumber": "09425144718",
      "LicenseNumber": "A01-24-000009",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "DO-OMA",
      "FirstName": "REGEI",
      "MiddleName": ""
    },
    {
      "Id": 19,
      "FullName": "ALMAREZ, JHON MICHAEL",
      "Role": "Aide",
      "ContactNumber": "09425144719",
      "LicenseNumber": "A01-24-000010",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "ALMAREZ",
      "FirstName": "JHON",
      "MiddleName": "MICHAEL"
    },
    {
      "Id": 20,
      "FullName": "BOLIBAR, JONI",
      "Role": "Driver",
      "ContactNumber": "09425144720",
      "LicenseNumber": "D01-24-000006",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "BOLIBAR",
      "FirstName": "JONI",
      "MiddleName": ""
    },
    {
      "Id": 21,
      "FullName": "CAPIENDO, PETRONILLO",
      "Role": "Driver",
      "ContactNumber": "09425144721",
      "LicenseNumber": "D01-24-000007",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "CAPIENDO",
      "FirstName": "PETRONILLO",
      "MiddleName": ""
    },
    {
      "Id": 22,
      "FullName": "DOMINGO, DANILO",
      "Role": "Aide",
      "ContactNumber": "09425144722",
      "LicenseNumber": "A01-24-000011",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "DOMINGO",
      "FirstName": "DANILO",
      "MiddleName": ""
    },
    {
      "Id": 23,
      "FullName": "NIEGOS, ELMER",
      "Role": "Aide",
      "ContactNumber": "09425144723",
      "LicenseNumber": "A01-24-000012",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "NIEGOS",
      "FirstName": "ELMER",
      "MiddleName": ""
    },
    {
      "Id": 24,
      "FullName": "NILLO, DOMINIC",
      "Role": "Aide",
      "ContactNumber": "09425144724",
      "LicenseNumber": "A01-24-000013",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "NILLO",
      "FirstName": "DOMINIC",
      "MiddleName": ""
    },
    {
      "Id": 25,
      "FullName": "NUGAS, REY",
      "Role": "Aide",
      "ContactNumber": "09425144725",
      "LicenseNumber": "A01-24-000014",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "NUGAS",
      "FirstName": "REY",
      "MiddleName": ""
    },
    {
      "Id": 26,
      "FullName": "SUMALABAG, ROMMEL",
      "Role": "Aide",
      "ContactNumber": "09425144726",
      "LicenseNumber": "A01-24-000015",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "SUMALABAG",
      "FirstName": "ROMMEL",
      "MiddleName": ""
    },
    {
      "Id": 27,
      "FullName": "VILLANUEVA, ARKEL",
      "Role": "Aide",
      "ContactNumber": "09425144727",
      "LicenseNumber": "A01-24-000016",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "VILLANUEVA",
      "FirstName": "ARKEL",
      "MiddleName": ""
    },
    {
      "Id": 28,
      "FullName": "SUMALBAG, JOLO",
      "Role": "Aide",
      "ContactNumber": "09425144728",
      "LicenseNumber": "A01-24-000017",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "SUMALBAG",
      "FirstName": "JOLO",
      "MiddleName": ""
    },
    {
      "Id": 29,
      "FullName": "CAMPANILLA C.",
      "Role": "Driver",
      "ContactNumber": "09425144729",
      "LicenseNumber": "D01-24-000008",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "C.",
      "FirstName": "CAMPANILLA",
      "MiddleName": ""
    },
    {
      "Id": 30,
      "FullName": "HIBAYA, CRISANTO",
      "Role": "Driver",
      "ContactNumber": "09425144730",
      "LicenseNumber": "D01-24-000009",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "HIBAYA",
      "FirstName": "CRISANTO",
      "MiddleName": ""
    },
    {
      "Id": 31,
      "FullName": "ABELLAR, ISRAEL",
      "Role": "COOR-RBI",
      "ContactNumber": "09425144731",
      "LicenseNumber": "A01-24-000018",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "",
      "IsSeasonal": 0,
      "LastName": "ABELLAR",
      "FirstName": "ISRAEL",
      "MiddleName": ""
    },
    {
      "Id": 32,
      "FullName": "DEL ROSARIO, JHON PAUL",
      "Role": "Driver",
      "ContactNumber": "09425144732",
      "LicenseNumber": "D01-24-000010",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "DEL ROSARIO",
      "FirstName": "JHON",
      "MiddleName": "PAUL"
    },
    {
      "Id": 33,
      "FullName": "ESTOESTA, RALPH",
      "Role": "Driver",
      "ContactNumber": "09425144733",
      "LicenseNumber": "D01-24-000011",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "ESTOESTA",
      "FirstName": "RALPH",
      "MiddleName": ""
    },
    {
      "Id": 34,
      "FullName": "GONZALES, ROMEO",
      "Role": "Driver",
      "ContactNumber": "09425144734",
      "LicenseNumber": "D01-24-000012",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "GONZALES",
      "FirstName": "ROMEO",
      "MiddleName": ""
    },
    {
      "Id": 35,
      "FullName": "LACADIN, MARK",
      "Role": "Driver",
      "ContactNumber": "09425144735",
      "LicenseNumber": "D01-24-000013",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "LACADIN",
      "FirstName": "MARK",
      "MiddleName": ""
    },
    {
      "Id": 36,
      "FullName": "RINGOR, REYNAR",
      "Role": "Driver",
      "ContactNumber": "09425144736",
      "LicenseNumber": "D01-24-000014",
      "DailyWage": 0.0,
      "FoodAllowance": 12.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "RINGOR",
      "FirstName": "REYNAR",
      "MiddleName": ""
    },
    {
      "Id": 37,
      "FullName": "YORO, JOSE CEAZAR S.",
      "Role": "Driver",
      "ContactNumber": "09425144737",
      "LicenseNumber": "D01-24-000015",
      "DailyWage": 0.0,
      "FoodAllowance": 15.0,
      "IsAvailable": 1,
      "IsActive": 1,
      "PhotoPath": "0",
      "IsSeasonal": 0,
      "LastName": "YORO",
      "FirstName": "JOSE CEAZAR",
      "MiddleName": "S."
    }
  ],
  "Trucks": [
    {
      "Id": 6,
      "PlateNumber": "NEG 1721",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 320,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 7,
      "PlateNumber": "CAE 3212",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 230,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 9,
      "PlateNumber": "CBP 2036",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 180,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 10,
      "PlateNumber": "CBS 3286",
      "TruckType": "FORWARD 8 TONS",
      "CapacityCrates": 350,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 11,
      "PlateNumber": "CBQ 3286",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 260,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 12,
      "PlateNumber": "CAA 3866",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 180,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 13,
      "PlateNumber": "CBA 2699",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 300,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 14,
      "PlateNumber": "ICC 8370",
      "TruckType": "FORWARD 8 TONS",
      "CapacityCrates": 360,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 15,
      "PlateNumber": "CCO 4913",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 80,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 16,
      "PlateNumber": "NBJ 8329",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 17,
      "PlateNumber": "NBJ 8330",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 18,
      "PlateNumber": "NBL 4805",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 19,
      "PlateNumber": "NAM 7985",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 20,
      "PlateNumber": "NBF 8965",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 21,
      "PlateNumber": "CAO1116",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 22,
      "PlateNumber": "WIH 342",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 23,
      "PlateNumber": "CBN 7056",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 24,
      "PlateNumber": "CBQ 4870",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 25,
      "PlateNumber": "CCE 9458",
      "TruckType": "FORWARD 4 TONS",
      "CapacityCrates": 100,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    },
    {
      "Id": 26,
      "PlateNumber": "AWA 4297",
      "TruckType": "FORWARD 6 TONS",
      "CapacityCrates": 200,
      "Status": "Available",
      "Remarks": "",
      "CargoType": "Cold / Chilled"
    }
  ],
  "RateMatrix": [
    {
      "Id": 7,
      "RouteName": "Route A - Pampanga Express",
      "Destination": "Pampanga Branches",
      "TruckType": "FORWARD 4 TONS",
      "IsPeakSeason": 0,
      "RateAmount": 8500.0
    },
    {
      "Id": 10,
      "RouteName": "Route B - Nueva Ecija Main",
      "Destination": "Cabanatuan / Gapan",
      "TruckType": "Refrigerated Van",
      "IsPeakSeason": 0,
      "RateAmount": 12500.0
    },
    {
      "Id": 11,
      "RouteName": "Route C - Northern Luzon",
      "Destination": "La Union / Ilocos Sur",
      "TruckType": "6-Wheeler Truck",
      "IsPeakSeason": 0,
      "RateAmount": 16000.0
    },
    {
      "Id": 12,
      "RouteName": "Route C - Northern Luzon",
      "Destination": "La Union / Ilocos Sur",
      "TruckType": "6-Wheeler Truck",
      "IsPeakSeason": 0,
      "RateAmount": 19500.0
    }
  ],
  "LoadingSchedules": [
    {
      "Id": 1,
      "ScheduleNumber": "LOAD-20260829-191345",
      "ClientName": "RED RIBBON BAKESHOP INC.",
      "Destination": "TABUK",
      "ScheduleDate": "2026-08-29",
      "TotalCrates": 230,
      "IsPeak": 0,
      "Status": "Completed",
      "Remarks": "Scheduled Delivery Order",
      "DepartureTime": "01:00 AM",
      "GCrate": 230,
      "ECrate": 0,
      "CancellationReason": "",
      "CargoType": "Cold / Chilled",
      "RescheduleCount": 0,
      "OriginalScheduleDate": null,
      "RescheduleReason": ""
    },
    {
      "Id": 2,
      "ScheduleNumber": "LS-20260920-174821-34158",
      "ClientName": "RED RIBBON BAKESHOP INC.",
      "Destination": "TABUK",
      "ScheduleDate": "2026-09-20",
      "TotalCrates": 230,
      "IsPeak": 0,
      "Status": "Pending",
      "Remarks": "",
      "DepartureTime": "01:00 AM",
      "GCrate": 230,
      "ECrate": 0,
      "CancellationReason": "",
      "CargoType": "Cold / Chilled",
      "RescheduleCount": 0,
      "OriginalScheduleDate": null,
      "RescheduleReason": ""
    },
    {
      "Id": 3,
      "ScheduleNumber": "LS-20260923-162107-26138",
      "ClientName": "RED RIBBON BAKESHOP INC.",
      "Destination": "TABUK",
      "ScheduleDate": "2026-09-23",
      "TotalCrates": 230,
      "IsPeak": 0,
      "Status": "Pending",
      "Remarks": "",
      "DepartureTime": "01:00 AM",
      "GCrate": 230,
      "ECrate": 0,
      "CancellationReason": "",
      "CargoType": "Cold / Chilled",
      "RescheduleCount": 0,
      "OriginalScheduleDate": null,
      "RescheduleReason": ""
    }
  ],
  "Deliveries": [
    {
      "Id": 2,
      "DispatchNumber": "DSP-20260901-140140",
      "LoadingScheduleId": 1,
      "LoadingScheduleInfo": "RED RIBBON BAKESHOP INC. (TABUK)",
      "TruckId": 25,
      "TruckInfo": "CCE 9458 (FORWARD 4 TONS)",
      "DriverId": 37,
      "DriverName": "YORO, JOSE CEAZAR S.",
      "AideId": 0,
      "AideName": "None",
      "DispatchDate": "2026-08-29",
      "DepartureTime": "08:00 pm",
      "Status": "Cancelled",
      "Remarks": "Dispatched Delivery Order | Rescheduled",
      "GCrate": 230,
      "ECrate": 0,
      "TotalCrates": 230,
      "AideIds": "",
      "AideNames": "None",
      "CancellationReason": "",
      "CargoType": "Cold / Chilled",
      "IsMultiDrop": 0,
      "TotalStopsCount": 1
    },
    {
      "Id": 3,
      "DispatchNumber": "DSP-20260901-140348",
      "LoadingScheduleId": 1,
      "LoadingScheduleInfo": "RED RIBBON BAKESHOP INC. (TABUK)",
      "TruckId": 26,
      "TruckInfo": "AWA 4297 (FORWARD 6 TONS)",
      "DriverId": 37,
      "DriverName": "YORO, JOSE CEAZAR S.",
      "AideId": 27,
      "AideName": "VILLANUEVA, ARKEL",
      "DispatchDate": "2026-09-05",
      "DepartureTime": "08:00 pm",
      "Status": "Completed",
      "Remarks": "Dispatched Delivery Order",
      "GCrate": 100,
      "ECrate": 100,
      "TotalCrates": 200,
      "AideIds": "27,26",
      "AideNames": "VILLANUEVA, ARKEL, SUMALABAG, ROMMEL",
      "CancellationReason": "",
      "CargoType": "Cold / Chilled",
      "IsMultiDrop": 0,
      "TotalStopsCount": 1
    }
  ],
  "TripReports": [
    {
      "Id": 1,
      "TripReportNumber": "TRIP-20260901-081407",
      "DeliveryId": 1,
      "DispatchNumber": "DSP-20260829-193458",
      "ArrivalTime": "10:30 AM",
      "DepartureTime": "11:45 AM",
      "DeliveredCrates": 150,
      "ReturnedCrates": 150,
      "StoreManagerName": "maricel de ocampo",
      "Remarks": "",
      "ReportDate": "2026-09-01",
      "DeliveredGCrates": 150,
      "DeliveredECrates": 0,
      "ReturnedGCrates": 150,
      "ReturnedECrates": 0,
      "StoreManagerLastName": "de ocampo",
      "StoreManagerFirstName": "maricel",
      "StoreManagerMiddleName": ""
    },
    {
      "Id": 2,
      "TripReportNumber": "TRIP-20260904-224555",
      "DeliveryId": 3,
      "DispatchNumber": "DSP-20260901-140348",
      "ArrivalTime": "10:30 AM",
      "DepartureTime": "11:45 AM",
      "DeliveredCrates": 150,
      "ReturnedCrates": 150,
      "StoreManagerName": "maricel de ocampo",
      "Remarks": "",
      "ReportDate": "2026-09-04",
      "DeliveredGCrates": 150,
      "DeliveredECrates": 0,
      "ReturnedGCrates": 150,
      "ReturnedECrates": 0,
      "StoreManagerLastName": "de ocampo",
      "StoreManagerFirstName": "maricel",
      "StoreManagerMiddleName": ""
    }
  ],
  "IncidentReports": [
    {
      "Id": 1,
      "DeliveryId": 3,
      "DispatchNumber": "DSP-20260901-140348",
      "IncidentDate": "2026-09-04",
      "DamagedQuantity": 50,
      "EstimatedCost": 22500.0,
      "Cause": "Missing / Damaged Crates from Trip Delivery",
      "ChargeableToCrew": 1,
      "Description": "Automated variance log: 50 crate(s) discrepancy noted on delivery DSP-20260901-140348 on 09/04/2026.",
      "Status": "Logged",
      "PhotoPath": ""
    }
  ],
  "StatementsOfAccount": [],
  "Invoices": [
    {
      "Id": 1,
      "InvoiceNumber": "INV-20260923-161652-85947",
      "SoaId": 0,
      "DeliveryId": 3,
      "ClientName": "RED RIBBON BAKESHOP INC.",
      "IssueDate": "2026-09-23",
      "DueDate": "2026-10-08",
      "TotalAmount": 9000.0,
      "Deductions": 0.0,
      "NetAmount": 9000.0,
      "Status": "Paid"
    }
  ],
  "CollectionReceipts": [
    {
      "Id": 1,
      "ReceiptNumber": "CR-20260923-161720-08450",
      "InvoiceId": 1,
      "InvoiceNumber": "INV-20260923-161652-85947",
      "PaymentDate": "2026-09-23",
      "AmountPaid": 9000.0,
      "PaymentMethod": "CHECK",
      "ReferenceNumber": "",
      "Notes": ""
    }
  ],
  "LookupCategories": [
    {
      "Id": 3,
      "CategoryType": "Truck Type / Category",
      "ItemValue": "FORWARD 6 TONS",
      "ExtraValue": null
    },
    {
      "Id": 11,
      "CategoryType": "Truck Type / Category",
      "ItemValue": "FORWARD 8 TONS",
      "ExtraValue": null
    },
    {
      "Id": 18,
      "CategoryType": "Truck Type / Category",
      "ItemValue": "FORWARD 4 TONS",
      "ExtraValue": null
    },
    {
      "Id": 31,
      "CategoryType": "Client Company",
      "ItemValue": "BCS GROUP COMPANY",
      "ExtraValue": null
    },
    {
      "Id": 32,
      "CategoryType": "Client Company",
      "ItemValue": "MILLENIUM FOOD BITES CORPORATION",
      "ExtraValue": null
    },
    {
      "Id": 33,
      "CategoryType": "Client Company",
      "ItemValue": "RED RIBBON BAKESHOP INC.",
      "ExtraValue": null
    },
    {
      "Id": 34,
      "CategoryType": "Destination Route",
      "ItemValue": "LOAG",
      "ExtraValue": null
    },
    {
      "Id": 35,
      "CategoryType": "Destination Route",
      "ItemValue": "LA UNION",
      "ExtraValue": null
    },
    {
      "Id": 36,
      "CategoryType": "Destination Route",
      "ItemValue": "LA TRINIDAD",
      "ExtraValue": null
    },
    {
      "Id": 37,
      "CategoryType": "Destination Route",
      "ItemValue": "TABUK",
      "ExtraValue": null
    },
    {
      "Id": 38,
      "CategoryType": "Destination Route",
      "ItemValue": "ISABELA",
      "ExtraValue": null
    },
    {
      "Id": 39,
      "CategoryType": "Destination Route",
      "ItemValue": "PANGASINAN",
      "ExtraValue": null
    },
    {
      "Id": 40,
      "CategoryType": "Destination Route",
      "ItemValue": "PAMPANGA",
      "ExtraValue": null
    },
    {
      "Id": 43,
      "CategoryType": "Crew Role",
      "ItemValue": "CHECKER",
      "ExtraValue": null
    },
    {
      "Id": 47,
      "CategoryType": "Crew Role",
      "ItemValue": "MECHANIC",
      "ExtraValue": null
    },
    {
      "Id": 78,
      "CategoryType": "Crew Role",
      "ItemValue": "COOR-RBI",
      "ExtraValue": null
    },
    {
      "Id": 86,
      "CategoryType": "Payment Method",
      "ItemValue": "CHECK",
      "ExtraValue": null
    },
    {
      "Id": 87,
      "CategoryType": "Payment Method",
      "ItemValue": "G-CASH",
      "ExtraValue": null
    },
    {
      "Id": 88,
      "CategoryType": "Payment Method",
      "ItemValue": "CASH",
      "ExtraValue": null
    },
    {
      "Id": 97,
      "CategoryType": "Crew Role",
      "ItemValue": "Driver",
      "ExtraValue": null
    },
    {
      "Id": 99,
      "CategoryType": "Crew Role",
      "ItemValue": "Aide",
      "ExtraValue": null
    },
    {
      "Id": 100,
      "CategoryType": "Crew Role",
      "ItemValue": "Checker",
      "ExtraValue": null
    },
    {
      "Id": 113,
      "CategoryType": "Crew Role",
      "ItemValue": "Mechanic",
      "ExtraValue": null
    },
    {
      "Id": 141,
      "CategoryType": "Destination Route",
      "ItemValue": "SAN VARGA",
      "ExtraValue": null
    }
  ],
  "Clients": [
    {
      "Id": 1,
      "CompanyName": "BCS GROUP COMPANY",
      "ContactPerson": "",
      "ContactNumber": "",
      "Address": "",
      "Email": "",
      "LastName": "",
      "FirstName": "",
      "MiddleName": ""
    },
    {
      "Id": 2,
      "CompanyName": "RED RIBBON BAKESHOP INC.",
      "ContactPerson": "",
      "ContactNumber": "",
      "Address": "",
      "Email": "",
      "LastName": "",
      "FirstName": "",
      "MiddleName": ""
    },
    {
      "Id": 3,
      "CompanyName": "MILLENIUM FOOD BITES CORPORATION",
      "ContactPerson": "",
      "ContactNumber": "",
      "Address": "",
      "Email": "",
      "LastName": "",
      "FirstName": "",
      "MiddleName": ""
    }
  ],
  "LoginAudit": [
    {
      "Id": 1,
      "Timestamp": "2026-09-04 22:27:04",
      "Username": "HARMIN",
      "EventType": "ACCOUNT_REGISTERED",
      "IpOrMachine": "HAZELLEA",
      "Details": "Staff self-registration: HARMIN DELA VASQUEZ (9876755125)"
    },
    {
      "Id": 2,
      "Timestamp": "2026-09-04 22:27:30",
      "Username": "Harmin",
      "EventType": "LOGIN_BLOCKED_PENDING",
      "IpOrMachine": "HAZELLEA",
      "Details": "Pending staff user attempted to login"
    },
    {
      "Id": 3,
      "Timestamp": "2026-09-04 22:27:48",
      "Username": "Admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 4,
      "Timestamp": "2026-09-15 14:11:30",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 5,
      "Timestamp": "2026-09-15 17:22:14",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 6,
      "Timestamp": "2026-09-16 12:59:57",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 7,
      "Timestamp": "2026-09-16 13:18:10",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 8,
      "Timestamp": "2026-09-17 00:23:27",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 9,
      "Timestamp": "2026-09-17 07:31:54",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 10,
      "Timestamp": "2026-09-17 11:13:51",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 11,
      "Timestamp": "2026-09-17 12:49:35",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 12,
      "Timestamp": "2026-09-17 13:33:11",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 13,
      "Timestamp": "2026-09-17 13:49:00",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 14,
      "Timestamp": "2026-09-17 14:18:13",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 15,
      "Timestamp": "2026-09-19 03:02:36",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 16,
      "Timestamp": "2026-09-19 03:27:40",
      "Username": "admin",
      "EventType": "LOGIN_FAILED",
      "IpOrMachine": "HAZELLEA",
      "Details": "Invalid login attempt 1 of 5"
    },
    {
      "Id": 17,
      "Timestamp": "2026-09-19 03:27:48",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 18,
      "Timestamp": "2026-09-19 03:44:56",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 19,
      "Timestamp": "2026-09-20 11:00:00",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 20,
      "Timestamp": "2026-09-20 13:27:15",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 21,
      "Timestamp": "2026-09-23 14:41:25",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    },
    {
      "Id": 22,
      "Timestamp": "2026-09-23 16:28:59",
      "Username": "admin",
      "EventType": "LOGIN_SUCCESS",
      "IpOrMachine": "HAZELLEA",
      "Details": "User successfully authenticated (Owner/Admin)"
    }
  ],
  "SystemSettings": [
    {
      "SettingKey": "AllowMultiDestDispatch",
      "SettingValue": "True",
      "Description": "Multi-drop dispatch preference",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CompanyName",
      "SettingValue": "M.J. Billones Transport Service",
      "Description": "Official business name",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CompanyTagline",
      "SettingValue": "Reliable Cold Chain & Freight Logistics",
      "Description": "Company slogan",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CompanyContact",
      "SettingValue": "0917-123-4567",
      "Description": "Company contact number",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CompanyTin",
      "SettingValue": "123-456-789-000",
      "Description": "Tax identification number",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CompanyAddress",
      "SettingValue": "Cavite / Batangas Operations Depot",
      "Description": "Depot and warehouse address",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "InvoiceFooter",
      "SettingValue": "Thank you for your business! All deliveries are subject to standard freight terms.",
      "Description": "Footer note on printouts",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "DefaultDepartureTime",
      "SettingValue": "08:00 AM",
      "Description": "Default dispatch departure time",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "AutoRefreshRate",
      "SettingValue": "Every 60 Seconds",
      "Description": "Table grid refresh frequency",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "RequireCrateReturnTracking",
      "SettingValue": "True",
      "Description": "Empty crate return tracking",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "AutoFleetRecommendation",
      "SettingValue": "True",
      "Description": "Truck recommendation helper",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "CurrencySymbol",
      "SettingValue": "PHP (Philippine Peso)",
      "Description": "Default currency representation",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "DateFormat",
      "SettingValue": "yyyy-MM-dd (ISO Standard)",
      "Description": "Standard date format",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "EnableToastPopups",
      "SettingValue": "True",
      "Description": "Enable notification toast popups",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "StartMenuCollapsed",
      "SettingValue": "True",
      "Description": "Start sidebar menu in collapsed state",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "EnableProactiveAlerts",
      "SettingValue": "True",
      "Description": "Automated truck & aging invoice alerts",
      "UpdatedAt": "2026-09-23 02:47:11"
    },
    {
      "SettingKey": "NotifyLowFleet",
      "SettingValue": "True",
      "Description": "Alert when available trucks fall to 1 vehicle",
      "UpdatedAt": "2026-09-23 02:47:11"
    }
  ],
  "FormDrafts": [
    {
      "ModuleKey": "IncidentReport",
      "UserKey": "admin",
      "DraftData": "{\"DeliveryIndex\":\"0\",\"DamagedQuantity\":\"2\",\"EstimatedCost\":\"1500\",\"Cause\":\"\",\"Chargeable\":\"False\",\"Description\":\"\",\"PhotoPath\":\"\"}",
      "LastSaved": "2026-09-23 16:28:07"
    }
  ],
  "AuditTrail": [
    {
      "Id": 1,
      "Timestamp": "2026-09-20 11:04:01",
      "Username": "admin",
      "UserRole": "Owner/Admin",
      "ActionType": "SECURITY_UPDATE",
      "TableName": "Users",
      "RecordId": 1,
      "RecordIdentifier": "User #1",
      "FieldName": "SecurityQuestion",
      "OldValue": "",
      "NewValue": "mothers maiden name",
      "ReasonOrNotes": "Security recovery question configured/updated",
      "IpOrMachine": "HAZELLEA"
    },
    {
      "Id": 2,
      "Timestamp": "2026-09-23 14:41:49",
      "Username": "admin",
      "UserRole": "Owner/Admin",
      "ActionType": "SECURITY_UPDATE",
      "TableName": "Users",
      "RecordId": 1,
      "RecordIdentifier": "User #1",
      "FieldName": "SecurityQuestion",
      "OldValue": "",
      "NewValue": "mothers maiden name",
      "ReasonOrNotes": "Security recovery question configured/updated",
      "IpOrMachine": "HAZELLEA"
    },
    {
      "Id": 3,
      "Timestamp": "2026-09-23 16:16:52",
      "Username": "admin",
      "UserRole": "Owner/Admin",
      "ActionType": "INVOICE_CREATED",
      "TableName": "Invoices",
      "RecordId": 0,
      "RecordIdentifier": "INV-20260923-161652-85947",
      "FieldName": "NetAmount",
      "OldValue": "",
      "NewValue": "9000.00",
      "ReasonOrNotes": "Invoice created for RED RIBBON BAKESHOP INC., Status: Unpaid, Net: PHP 9,000.00",
      "IpOrMachine": "HAZELLEA"
    },
    {
      "Id": 4,
      "Timestamp": "2026-09-23 16:17:20",
      "Username": "admin",
      "UserRole": "Owner/Admin",
      "ActionType": "PAYMENT_COLLECTED",
      "TableName": "CollectionReceipts",
      "RecordId": 0,
      "RecordIdentifier": "CR-20260923-161720-08450",
      "FieldName": "AmountPaid",
      "OldValue": "",
      "NewValue": "PHP 9,000.00",
      "ReasonOrNotes": "Collection receipt for Invoice #INV-20260923-161652-85947 via CHECK (Ref: )",
      "IpOrMachine": "HAZELLEA"
    }
  ],
  "SystemNotifications": [
    {
      "Id": "e066a193-79e3-4801-948d-a44d46d4919e",
      "Title": "System Online",
      "Message": "SQLite database connected and operating with full data integrity.",
      "Timestamp": "2026-09-20T10:55:00.9886006+08:00",
      "Type": "Success",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": "Settings",
      "TargetRecordId": null
    },
    {
      "Id": "433957cf-d686-4a23-9d74-f6e2950f4cfa",
      "Title": "Auto Backup Verified",
      "Message": "Automated data backup engine initialized and standing by.",
      "Timestamp": "2026-09-20T10:58:00.9887874+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": "Settings",
      "TargetRecordId": null
    },
    {
      "Id": "6aba35e5-465f-4e9b-88b0-4448a4ed6300",
      "Title": "Session Active",
      "Message": "Authenticated as @admin (Owner/Admin)",
      "Timestamp": "2026-09-20T11:00:01.1172172+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "9b1517a4-5ce1-4193-961c-dc7cc4932ddd",
      "Title": "Security Updated",
      "Message": "Security settings updated for @admin",
      "Timestamp": "2026-09-20T11:04:01.3738083+08:00",
      "Type": "Success",
      "Category": "Security",
      "IsRead": 1,
      "TargetModule": "Settings",
      "TargetRecordId": null
    },
    {
      "Id": "578db666-53ee-4a85-ae94-30b05f22a2dc",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T11:30:00.9234423+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "a34d2bbd-53f7-40a6-821e-49b9b17c05e2",
      "Title": "Session Active",
      "Message": "Authenticated as @admin (Owner/Admin)",
      "Timestamp": "2026-09-20T13:27:16.0715228+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "87710d8e-edb3-4432-8588-8190395c0e96",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T13:57:15.8886039+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "9e42430f-5a5c-4f5c-a033-7ec2a2b0e62d",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T14:27:15.8642787+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "ee640661-5552-439d-84ff-a2da6e9aa5de",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T14:57:15.8733069+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "01078ee4-f974-42ab-8e85-afe28aacc2cc",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T15:27:15.8768989+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "72e57578-e99a-46b4-901f-79601858c417",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T15:57:15.8858324+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "f06d83ef-0ca6-4e96-bb10-a9746c90ee5b",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T16:27:15.8696392+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "1361fc15-a235-49f7-bae2-d172e832d0c7",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T16:57:15.8900837+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "35e9d892-dff4-4818-bb2b-31de910e82d4",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T17:27:15.8655110+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "43028d81-ef4d-41c4-b2e9-58433eadfec0",
      "Title": "Loading Order Created",
      "Message": "Order #LS-20260920-174821-34158 for RED RIBBON BAKESHOP INC. (230 crates)",
      "Timestamp": "2026-09-20T17:48:21.3542887+08:00",
      "Type": "Success",
      "Category": "Operations",
      "IsRead": 1,
      "TargetModule": "Loading",
      "TargetRecordId": null
    },
    {
      "Id": "993f29b5-9b52-454e-8c8c-72cccb79e1e8",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T17:57:15.8882670+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "c92d395f-4ecd-4d27-93f4-49c08d2c5819",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-20T18:27:15.8930146+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "0c0007a8-9377-4cc8-a003-28e92fe63583",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T10:29:16.8213688+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "6ababb1c-116e-4d12-898e-faa7875606ca",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T10:59:19.9340691+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "36634fbe-e484-4cbe-a7a1-d774d019e514",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T11:29:19.9334349+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "4f0d41ed-f75d-4a70-a44e-0b15733f6b21",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T11:59:19.9250485+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "c5a4f7f4-e318-4c0f-a596-8f53fbeee860",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T12:29:19.9279982+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "824a6d6b-88f8-45bc-99ef-291b166d5d76",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T12:59:19.9752125+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "f238f1bb-2e77-42fd-b703-48cb15062e63",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T13:29:19.9302378+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "54656414-32d3-436e-96aa-e1d925cb1fe9",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T13:59:19.9269271+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "28ddb085-daef-4b23-aa5c-fb602e307704",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T14:29:19.9330771+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "f85b02b0-b382-4b3c-831b-2369564a6b63",
      "Title": "Session Active",
      "Message": "Authenticated as @admin (Owner/Admin)",
      "Timestamp": "2026-09-23T14:41:26.3470928+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "b73bfb4b-a911-4da5-aa4e-61e6bccbe6ec",
      "Title": "Security Updated",
      "Message": "Security settings updated for @admin",
      "Timestamp": "2026-09-23T14:41:49.9829802+08:00",
      "Type": "Success",
      "Category": "Security",
      "IsRead": 1,
      "TargetModule": "Settings",
      "TargetRecordId": null
    },
    {
      "Id": "a75a835e-0a21-4640-8e23-e4ddc83b6cda",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T15:11:26.1623544+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 1,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "01e50834-2827-48c8-ac8d-4ce8b7dd0955",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T15:41:26.1519442+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 0,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "270d0758-70f7-4ad1-92fa-500f9529e8e2",
      "Title": "Database Snapshot",
      "Message": "Automatic background database backup snapshot created.",
      "Timestamp": "2026-09-23T16:11:26.1619545+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 0,
      "TargetModule": null,
      "TargetRecordId": null
    },
    {
      "Id": "baf43696-4cbe-4116-a0e6-5dcf06ae3e00",
      "Title": "Invoice Created",
      "Message": "Invoice #INV-20260923-161652-85947 for RED RIBBON BAKESHOP INC. (PHP 9,000.00)",
      "Timestamp": "2026-09-23T16:16:52.8996782+08:00",
      "Type": "Success",
      "Category": "Payments",
      "IsRead": 0,
      "TargetModule": "Invoices",
      "TargetRecordId": null
    },
    {
      "Id": "9902de67-95e5-49b6-88db-3b4c959dd3b1",
      "Title": "Payment Collected",
      "Message": "Receipt #CR-20260923-161720-08450 for invoice #INV-20260923-161652-85947 (PHP 9,000.00)",
      "Timestamp": "2026-09-23T16:17:20.0883859+08:00",
      "Type": "Success",
      "Category": "Payments",
      "IsRead": 1,
      "TargetModule": "Collections",
      "TargetRecordId": null
    },
    {
      "Id": "17f90e0f-dbb4-4296-bb20-4a50acc63295",
      "Title": "Loading Order Created",
      "Message": "Order #LS-20260923-162107-26138 for RED RIBBON BAKESHOP INC. (230 crates)",
      "Timestamp": "2026-09-23T16:21:07.2900886+08:00",
      "Type": "Success",
      "Category": "Operations",
      "IsRead": 0,
      "TargetModule": "Loading",
      "TargetRecordId": null
    },
    {
      "Id": "17066014-3f85-4bc2-810c-435ae62753ba",
      "Title": "Session Active",
      "Message": "Authenticated as @admin (Owner/Admin)",
      "Timestamp": "2026-09-23T16:28:59.9483213+08:00",
      "Type": "Info",
      "Category": "System",
      "IsRead": 0,
      "TargetModule": null,
      "TargetRecordId": null
    }
  ],
  "DispatchStops": []
};

if (typeof module !== 'undefined' && module.exports) {
  module.exports = window.INITIAL_SEED_DATA;
}
