import os
import sys
import time
import pyaudio
from pocketsphinx import Pocketsphinx, get_model_path
import math

# Set up logging
script_dir = os.path.dirname(os.path.abspath(__file__))
log_file_path = os.path.join(script_dir, "phoneme_recognition_log.txt")

def log_message(message):
    with open(log_file_path, "a") as log_file:
        log_file.write(f"{message}\n")
        log_file.flush()  # Ensure immediate writing to file
    print(message, flush=True)  # Ensure immediate console output

log_message("Python script started")

try:
    # Create custom dictionary
    dict_path = os.path.join(script_dir, "custom_phonemes.dict")
    with open(dict_path, 'w') as f:
        f.write("AH AH\n")
        f.write("BUH B AH\n")
        f.write("CUH K AH\n")
    log_message("Custom dictionary created")

    # Create custom language model
    lm_path = os.path.join(script_dir, "custom_phonemes.lm")
    with open(lm_path, 'w') as f:
        f.write("""
\\data\\
ngram 1=5

\\1-grams:
-1.0000 </s>
-1.0000 <s>
-1.0000 AH
-1.0000 BUH
-1.0000 CUH

\\end\\
""")
    log_message("Custom language model created")

    # Initialize PocketSphinx
    model_path = get_model_path()
    ps = Pocketsphinx(
        hmm=os.path.join(model_path, 'en-us'),
        lm=lm_path,
        dict=dict_path,
        samprate=16000
    )
    log_message("PocketSphinx initialized")

    # Initialize PyAudio
    p = pyaudio.PyAudio()
    stream = p.open(format=pyaudio.paInt16, channels=1, rate=16000, input=True, frames_per_buffer=1024)
    log_message("PyAudio stream opened")

    log_message("Starting phoneme recognition")

    # Start the utterance
    ps.start_utt()

    try:
        while True:
            buf = stream.read(1024, exception_on_overflow=False)
            if buf:
                ps.process_raw(buf, False, False)
            else:
                break
            
            hypothesis = ps.hypothesis()
            if hypothesis:
                n_frames = ps.n_frames()
                
                if n_frames >= 200:
                    log_prob = ps.get_prob()
                    confidence = 1 - (1 / (1 + math.exp(log_prob)))
                    
                    log_message(f"Recognized: {hypothesis}")
                    log_message(f"Confidence: {confidence:.4f}")
                    log_message(f"Frames: {n_frames}")
                    log_message("---")
                
                ps.end_utt()
                ps.start_utt()

    except KeyboardInterrupt:
        log_message("Stopped listening")
    except Exception as e:
        log_message(f"Error during recognition: {str(e)}")
    finally:
        stream.stop_stream()
        stream.close()
        p.terminate()
        ps.end_utt()
        log_message("Audio stream closed")

except Exception as e:
    log_message(f"Setup error: {str(e)}")

log_message("Python script finished")