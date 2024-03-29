find_package(Qt4 REQUIRED COMPONENTS QtCore QtXmlPatterns)
find_package(Smoke REQUIRED COMPONENTS QtXmlPatterns)

# source files
SET(SRC_QYOTO_QTXMLPATTERNS_NATIVE
    qtxmlpatterns.cpp
)

INCLUDE_DIRECTORIES (${QT_INCLUDES} ${SMOKE_INCLUDE_DIR} ${SMOKE_QTXMLPATTERNS_INCLUDE_DIR}/smoke ../../qyoto-qtcore/native)

ADD_LIBRARY (qyoto-qtxmlpatterns-native SHARED ${SRC_QYOTO_QTXMLPATTERNS_NATIVE})
TARGET_LINK_LIBRARIES (qyoto-qtxmlpatterns-native qyoto-qtcore-native ${SMOKE_BASE_LIBRARY} ${SMOKE_QTXMLPATTERNS_LIBRARY} ${QT_QTCORE_LIBRARY} ${QT_QTXMLPATTERNS_LIBRARY})


INSTALL(TARGETS qyoto-qtxmlpatterns-native LIBRARY DESTINATION ${LIB_INSTALL_DIR}
						  				   ARCHIVE DESTINATION ${LIB_INSTALL_DIR}
						  				   RUNTIME DESTINATION bin)
if (WIN32)
	# Realign the stack, for compatibility with an older ABI.
	set_target_properties (qyoto-qtxmlpatterns-native PROPERTIES COMPILE_FLAGS -mstackrealign)

    # Get rid of the "lib" prefix on archives/DLLs in Windows.
    set_target_properties(qyoto-qtxmlpatterns-native
        PROPERTIES PREFIX "" IMPORT_PREFIX "")
endif (WIN32)
