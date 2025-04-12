import librosa
import json
import numpy as np

# Load the song
song_path = "SongA.mp3"  # Make sure this is your correct file path
y, sr = librosa.load(song_path)

# Tempo and Beat Frames
tempo, beat_frames = librosa.beat.beat_track(y=y, sr=sr)

# Onsets
onset_env = librosa.onset.onset_strength(y=y, sr=sr)
onset_times = librosa.onset.onset_detect(onset_envelope=onset_env, sr=sr, units='time')

# MFCC (Mel-frequency cepstral coefficients)
mfccs = librosa.feature.mfcc(y=y, sr=sr)

# Reduce the MFCC dimensions (keep only the first 10 coefficients, for example)
mfccs = mfccs[:10]  # Keep the first 10 MFCC coefficients

# Function to convert all ndarrays to lists in a dict
def convert_ndarray_to_list(data):
    if isinstance(data, np.ndarray):
        return data.tolist()  # Convert ndarray to list
    elif isinstance(data, dict):
        return {key: convert_ndarray_to_list(value) for key, value in data.items()}
    elif isinstance(data, list):
        return [convert_ndarray_to_list(item) for item in data]
    else:
        return data

# Prepare data to be saved in JSON format
data = {
    'tempo': float(tempo),  # Convert tempo to float (in case it's stored as a list with one value)
    'beat_frames': beat_frames,  # beat_frames is an ndarray, will be converted
    'onset_times': onset_times,  # onset_times is an ndarray, will be converted
    'mfccs': mfccs  # mfccs is an ndarray, will be converted
}

# Convert ndarrays inside the data dict to lists
data = convert_ndarray_to_list(data)

# Save the data to a JSON file
with open("song_data.json", 'w') as json_file:
    json.dump(data, json_file, indent=4)

print("Data saved to song_data.json")
