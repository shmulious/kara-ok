import sys
import logging

def setup_global_error_handling():
    def handle_unhandled_exception(exc_type, exc_value, exc_traceback):
        if issubclass(exc_type, KeyboardInterrupt):
            sys.__excepthook__(exc_type, exc_value, exc_traceback)
            return
        logging.error("Unhandled exception occurred", exc_info=(exc_type, exc_value, exc_traceback))

    logging.basicConfig(stream=sys.stderr, level=logging.ERROR, format='%(asctime)s - %(levelname)s - %(message)s')
    sys.excepthook = handle_unhandled_exception
