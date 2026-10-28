path = 'Database/MJdatabase.cs'
with open(path, 'r', encoding='utf-8') as f:
    text = f.read()

# 1. Cut RescheduleSpecialTrip from where it was placed
spec_trip_start = '        public static (bool Success, string Message) RescheduleSpecialTrip(int tripId, DateTime newDate'
idx_start = text.find(spec_trip_start)
if idx_start == -1:
    raise Exception("spec_trip_start not found")

# The end of RescheduleSpecialTrip is where it closes with '        }'
spec_trip_end_marker = 'return (false, $"Failed to reschedule special trip: {ex.Message}");\n                    }\n                }\n            }\n        }'
idx_end = text.find(spec_trip_end_marker, idx_start)
if idx_end == -1:
    # try CRLF
    spec_trip_end_marker = spec_trip_end_marker.replace('\n', '\r\n')
    idx_end = text.find(spec_trip_end_marker, idx_start)

if idx_end == -1:
    raise Exception("spec_trip_end_marker not found")

idx_end += len(spec_trip_end_marker)

spec_trip_code = text[idx_start:idx_end]

# Remove it from text
text = text[:idx_start] + text[idx_end:]

# Now check around RescheduleLoadingSchedule closing
# We need to make sure RescheduleLoadingSchedule closes properly with:
#                     }
#                 }
#             }
#         }

# Let's find after line 6433 of RescheduleDelivery:
marker_deliv_end = 'return (false, $"Reschedule failed: {ex.Message}");\n\n                    }\n\n                }\n\n            }\n\n        }'
if marker_deliv_end not in text:
    marker_deliv_end = marker_deliv_end.replace('\n', '\r\n')

if marker_deliv_end in text:
    text = text.replace(marker_deliv_end, marker_deliv_end + '\n\n' + spec_trip_code, 1)
    print("Placed RescheduleSpecialTrip after RescheduleDelivery")
else:
    # Let's search using a more flexible pattern
    find_str = 'return (true, $"Dispatch #{dspNum} successfully rescheduled to'
    idx_deliv = text.find(find_str)
    if idx_deliv != -1:
        # Find closing of RescheduleDelivery
        idx_catch = text.find('catch (Exception ex)', idx_deliv)
        # Find 4 closing braces
        curr = idx_catch
        for _ in range(4):
            curr = text.find('}', curr) + 1
        text = text[:curr] + '\n\n' + spec_trip_code + text[curr:]
        print("Placed RescheduleSpecialTrip after RescheduleDelivery via index")
    else:
        raise Exception("Delivery end not found")

with open(path, 'w', encoding='utf-8') as f:
    f.write(text)

print("MJdatabase.cs brackets fixed!")
