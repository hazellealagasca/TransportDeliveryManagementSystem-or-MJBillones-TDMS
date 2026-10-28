import os

path = r'Program.cs'
with open(path, 'rb') as f:
    data = f.read()

target = b'MessageBox.Show(\r\n                $"An unexpected system error occurred:{Environment.NewLine}\r\n    }\r\n}'
replacement = b'''MessageBox.Show(
                $"An unexpected system error occurred:{Environment.NewLine}{Environment.NewLine}{ex.Message}{Environment.NewLine}{Environment.NewLine}Details have been recorded in the local log.",
                "System Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
        }
    }
}'''

if target in data:
    data = data.replace(target, replacement.replace(b'\n', b'\r\n'))
    with open(path, 'wb') as f:
        f.write(data)
    print('SUCCESS')
else:
    print('TARGET NOT FOUND')
