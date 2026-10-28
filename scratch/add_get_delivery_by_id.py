path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

target = 'public static List<Delivery> GetDeliveries()'
rep = '''public static Delivery? GetDeliveryById(int id)
        {
            if (id <= 0) return null;
            return GetDeliveries().FirstOrDefault(d => d.Id == id);
        }

        public static List<Delivery> GetDeliveries()'''

if target in text:
    text = text.replace(target, rep, 1)
    with open(path, 'w', encoding='utf-8') as f:
        f.write(text)
    print('Added GetDeliveryById successfully')
else:
    print('target not found')
