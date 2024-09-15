# logger.py
import logging

def setup_logging(log_file='smule.log'):
    """Set up logging configuration."""
    logging.basicConfig(filename=log_file,
                        level=logging.INFO,
                        format='%(asctime)s - %(message)s',
                        datefmt='%Y-%m-%d %H:%M:%S')

def log_message(message):
    """Log a message to the log file."""
    logging.info(message)