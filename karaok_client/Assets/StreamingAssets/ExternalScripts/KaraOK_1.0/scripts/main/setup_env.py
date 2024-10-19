import sys
from installer import setup_environment

def main():
    if len(sys.argv) != 3:
        print("Usage: python3 setup_env.py <path_to_virtual_environment> <virtual_environment_name>")
        sys.exit(1)

    venv_path = sys.argv[1]
    venv_name = sys.argv[2]

    print(f"Running setup_environment with venv_path: {venv_path}, venv_name: {venv_name}")
    setup_environment(venv_path, venv_name)

if __name__ == "__main__":
    main()